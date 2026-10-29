using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using SmartOfficeSystem.Models;
using System;
using System.Collections.Generic;

namespace SmartOfficeSystem.Data
{
    public class ApplicationDbHandler
    {
        private readonly string? _connectionString;

        public ApplicationDbHandler(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // --- 1. AUTHENTICATION ---
        public string? GetUserRole(string username, string password)
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqliteCommand("SELECT Role FROM Users WHERE Username = @u AND Password = @p", conn);
                cmd.Parameters.AddWithValue("@u", username);
                cmd.Parameters.AddWithValue("@p", password);
                return cmd.ExecuteScalar()?.ToString();
            }
        }

        // --- 2. EMPLOYEE MANAGEMENT ---
        public List<Employee> GetEmployees()
        {
            var list = new List<Employee>();
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                string sql = @"SELECT e.Id, e.Name, e.Email, e.Department, e.Salary, 
                               (SELECT IFNULL(SUM(t.Progress) / COUNT(t.TaskId), 0) 
                                FROM Tasks t 
                                WHERE t.AssignedTo = e.Id) as AvgProgress 
                               FROM Employees e";
                var cmd = new SqliteCommand(sql, conn);
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        list.Add(new Employee {
                            Id = Convert.ToInt32(rdr["Id"]),
                            Name = rdr["Name"]?.ToString() ?? "",
                            Email = rdr["Email"]?.ToString() ?? "",
                            Department = rdr["Department"]?.ToString() ?? "",
                            Salary = rdr["Salary"] != DBNull.Value ? Convert.ToDouble(rdr["Salary"]) : 0.0,
                            Progress = rdr["AvgProgress"] != DBNull.Value ? Convert.ToInt32(rdr["AvgProgress"]) : 0
                        });
                    }
                }
            }
            return list;
        }

        public List<Employee> GetAllEmployees() => GetEmployees();

        public Employee? GetEmployeeById(int id)
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqliteCommand("SELECT * FROM Employees WHERE Id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                using (var rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        return new Employee {
                            Id = Convert.ToInt32(rdr["Id"]),
                            Name = rdr["Name"]?.ToString() ?? "",
                            Email = rdr["Email"]?.ToString() ?? "",
                            Department = rdr["Department"]?.ToString() ?? "",
                            Salary = Convert.ToDouble(rdr["Salary"])
                        };
                    }
                }
            }
            return null;
        }

        public bool AddEmployee(Employee emp)
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                var cmd = new SqliteCommand("INSERT INTO Employees (Name, Email, Department, Salary) VALUES (@n, @e, @d, @s)", conn);
                cmd.Parameters.AddWithValue("@n", emp.Name);
                cmd.Parameters.AddWithValue("@e", emp.Email);
                cmd.Parameters.AddWithValue("@d", emp.Department);
                cmd.Parameters.AddWithValue("@s", emp.Salary);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool UpdateEmployee(Employee emp)
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqliteCommand("UPDATE Employees SET Name=@n, Email=@e, Department=@d, Salary=@s WHERE Id=@id", conn);
                cmd.Parameters.AddWithValue("@n", emp.Name);
                cmd.Parameters.AddWithValue("@e", emp.Email);
                cmd.Parameters.AddWithValue("@d", emp.Department);
                cmd.Parameters.AddWithValue("@s", emp.Salary);
                cmd.Parameters.AddWithValue("@id", emp.Id);
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool DeleteEmployee(int id)
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqliteCommand("DELETE FROM Employees WHERE Id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        // --- 3. TASK MANAGEMENT ---
        public List<TaskModel> GetTasksForEmployee(string username)
        {
            var tasks = new List<TaskModel>();
            using (var conn = new SqliteConnection(_connectionString))
            {
                string query = @"SELECT t.TaskId, t.TaskName, t.Description, t.Status, t.Deadline, t.Priority, t.Progress 
                                 FROM Tasks t JOIN Users u ON t.AssignedTo = u.EmployeeId 
                                 WHERE u.Username = @u ORDER BY t.TaskId DESC";
                var cmd = new SqliteCommand(query, conn);
                cmd.Parameters.AddWithValue("@u", username);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tasks.Add(new TaskModel {
                            TaskId = reader.GetInt32(0),
                            TaskName = reader.GetString(1),
                            Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            Status = reader.GetString(3),
                            Deadline = DateTime.Parse(reader.GetString(4)),
                            Priority = reader.IsDBNull(5) ? "Medium" : reader.GetString(5),
                            Progress = reader.IsDBNull(6) ? 0 : reader.GetInt32(6)
                        });
                    }
                }
            }
            return tasks;
        }

        public int AssignTaskWithId(TaskModel task)
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                string query = "INSERT INTO Tasks (TaskName, Description, AssignedTo, AssignedBy, Deadline, Priority, Status, Progress) VALUES (@n, @desc, @to, @by, @date, @p, 'Pending', 0); SELECT last_insert_rowid();";
                var cmd = new SqliteCommand(query, conn);
                cmd.Parameters.AddWithValue("@n", task.TaskName);
                cmd.Parameters.AddWithValue("@desc", task.Description ?? "");
                cmd.Parameters.AddWithValue("@to", task.AssignedTo);
                cmd.Parameters.AddWithValue("@by", task.AssignedBy ?? "Manager");
                cmd.Parameters.AddWithValue("@date", task.Deadline.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@p", task.Priority ?? "Medium");
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public void SaveSubTasks(int taskId, List<string> subTasks)
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                foreach (var sub in subTasks)
                {
                    if (!string.IsNullOrEmpty(sub))
                    {
                        var cmd = new SqliteCommand("INSERT INTO SubTasks (MainTaskId, Description, IsCompleted) VALUES (@tid, @desc, 0)", conn);
                        cmd.Parameters.AddWithValue("@tid", taskId);
                        cmd.Parameters.AddWithValue("@desc", sub);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public List<SubTaskModel> GetSubTasksByTaskId(int taskId)
        {
            List<SubTaskModel> subTasks = new List<SubTaskModel>();
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqliteCommand("SELECT * FROM SubTasks WHERE MainTaskId = @tid", conn);
                cmd.Parameters.AddWithValue("@tid", taskId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        subTasks.Add(new SubTaskModel {
                            SubTaskId = Convert.ToInt32(reader["SubTaskId"]),
                            Description = reader["Description"].ToString(),
                            IsCompleted = Convert.ToInt32(reader["IsCompleted"]) == 1
                        });
                    }
                }
            }
            return subTasks;
        }

        public List<TaskModel> GetAllTasksWithEmployeeNames()
        {
            var list = new List<TaskModel>();
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqliteCommand("SELECT TaskId, TaskName, Status, Progress FROM Tasks ORDER BY TaskId DESC", conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new TaskModel {
                            TaskId = Convert.ToInt32(reader["TaskId"]),
                            TaskName = reader["TaskName"]?.ToString() ?? "",
                            Status = reader["Status"]?.ToString() ?? "Pending",
                            Progress = reader["Progress"] != DBNull.Value ? Convert.ToInt32(reader["Progress"]) : 0
                        });
                    }
                }
            }
            return list;
        }

        public void CompleteTask(int taskId)
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqliteCommand("UPDATE Tasks SET Status = 'Completed', Progress = 100 WHERE TaskId = @id", conn);
                cmd.Parameters.AddWithValue("@id", taskId);
                cmd.ExecuteNonQuery();
            }
        }

        public bool DeleteTask(int taskId)
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqliteCommand("DELETE FROM Tasks WHERE TaskId = @id", conn);
                cmd.Parameters.AddWithValue("@id", taskId);
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public void UpdateTaskProgress(int taskId, int progress)
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqliteCommand("UPDATE Tasks SET Progress = @p WHERE TaskId = @id", conn);
                cmd.Parameters.AddWithValue("@p", progress);
                cmd.Parameters.AddWithValue("@id", taskId);
                cmd.ExecuteNonQuery();
            }
        }

        // --- 4. LEAVE MANAGEMENT ---
        public bool ApplyLeave(LeaveModel lv)
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                string query = "INSERT INTO Leaves (EmployeeName, LeaveType, StartDate, EndDate, Reason, Status) VALUES (@name, @type, @start, @end, @reason, 'Pending')";
                var cmd = new SqliteCommand(query, conn);
                cmd.Parameters.AddWithValue("@name", lv.EmployeeName);
                cmd.Parameters.AddWithValue("@type", lv.LeaveType);
                cmd.Parameters.AddWithValue("@start", lv.StartDate.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@end", lv.EndDate.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@reason", lv.Reason ?? "");
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public List<LeaveModel> GetLeavesForEmployee(string employeeName)
        {
            var list = new List<LeaveModel>();
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqliteCommand("SELECT * FROM Leaves WHERE EmployeeName = @name ORDER BY LeaveId DESC", conn);
                cmd.Parameters.AddWithValue("@name", employeeName);
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        list.Add(new LeaveModel {
                            LeaveId = rdr.GetInt32(0),
                            EmployeeName = rdr.GetString(1),
                            LeaveType = rdr.GetString(2),
                            StartDate = DateTime.Parse(rdr.GetString(3)),
                            EndDate = DateTime.Parse(rdr.GetString(4)),
                            Reason = rdr.GetString(5),
                            Status = rdr.GetString(6)
                        });
                    }
                }
            }
            return list;
        }

        public List<LeaveModel> GetAllLeaves()
        {
            var list = new List<LeaveModel>();
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqliteCommand("SELECT * FROM Leaves ORDER BY LeaveId DESC", conn);
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        list.Add(new LeaveModel {
                            LeaveId = rdr.GetInt32(0),
                            EmployeeName = rdr.GetString(1),
                            LeaveType = rdr.GetString(2),
                            Status = rdr.GetString(6)
                        });
                    }
                }
            }
            return list;
        }

        public bool UpdateLeaveStatus(int leaveId, string status)
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqliteCommand("UPDATE Leaves SET Status = @s WHERE LeaveId = @id", conn);
                cmd.Parameters.AddWithValue("@s", status);
                cmd.Parameters.AddWithValue("@id", leaveId);
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        // --- 5. ATTENDANCE MANAGEMENT ---
        public bool IsCurrentlyPunchedIn(string username)
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                string today = DateTime.Now.ToString("yyyy-MM-dd");
                var cmd = new SqliteCommand("SELECT COUNT(*) FROM Attendance WHERE EmployeeName = @u AND Date = @d AND PunchOut IS NULL", conn);
                cmd.Parameters.AddWithValue("@u", username);
                cmd.Parameters.AddWithValue("@d", today);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        public List<AttendanceModel> GetAttendanceHistory(string username)
        {
            List<AttendanceModel> history = new List<AttendanceModel>();
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqliteCommand("SELECT * FROM Attendance WHERE EmployeeName = @u ORDER BY Date DESC", conn);
                cmd.Parameters.AddWithValue("@u", username);
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        history.Add(new AttendanceModel {
                            AttendanceId = Convert.ToInt32(rdr["AttendanceId"]),
                            Date = rdr["Date"]?.ToString(),
                            PunchIn = rdr["PunchIn"]?.ToString(),
                            PunchOut = rdr["PunchOut"]?.ToString(),
                            Status = rdr["Status"]?.ToString()
                        });
                    }
                }
            }
            return history;
        }

        public bool PunchIn(string name)
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqliteCommand("INSERT INTO Attendance (EmployeeName, Date, PunchIn, Status) VALUES (@n, @d, @t, 'Present')", conn);
                cmd.Parameters.AddWithValue("@n", name);
                cmd.Parameters.AddWithValue("@d", DateTime.Now.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@t", DateTime.Now.ToString("HH:mm:ss"));
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool PunchOut(string name)
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqliteCommand("UPDATE Attendance SET PunchOut = @t WHERE EmployeeName = @n AND Date = @d AND PunchOut IS NULL", conn);
                cmd.Parameters.AddWithValue("@t", DateTime.Now.ToString("HH:mm:ss"));
                cmd.Parameters.AddWithValue("@n", name);
                cmd.Parameters.AddWithValue("@d", DateTime.Now.ToString("yyyy-MM-dd"));
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool PunchInEnhanced(string username) => !IsCurrentlyPunchedIn(username) && PunchIn(username);
        public bool PunchOutEnhanced(string username) => PunchOut(username);

        public AttendanceModel? GetTodayAttendance(string username)
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqliteCommand("SELECT * FROM Attendance WHERE EmployeeName = @u AND Date = @d LIMIT 1", conn);
                cmd.Parameters.AddWithValue("@u", username);
                cmd.Parameters.AddWithValue("@d", DateTime.Now.ToString("yyyy-MM-dd"));
                using (var rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read()) {
                        return new AttendanceModel { PunchIn = rdr["PunchIn"]?.ToString(), PunchOut = rdr["PunchOut"]?.ToString() };
                    }
                }
            }
            return null;
        }

        // --- 6. DASHBOARD & NOTICES ---
        public dynamic GetDashboardStats()
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                int empCount = Convert.ToInt32(new SqliteCommand("SELECT COUNT(*) FROM Employees", conn).ExecuteScalar());
                return new { TotalEmployees = empCount, TotalSalary = 0, ActiveTasks = 0 };
            }
        }

        public List<string> GetActiveNotices()
        {
            var list = new List<string>();
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqliteCommand("SELECT Content FROM Notices ORDER BY PostedDate DESC LIMIT 5", conn);
                using (var rdr = cmd.ExecuteReader()) { while (rdr.Read()) list.Add(rdr[0]?.ToString() ?? ""); }
            }
            return list;
        }

        public void AddNotice(string message, string postedBy)
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqliteCommand("INSERT INTO Notices (Title, Content, PostedDate, PostedBy) VALUES ('Notice', @c, @d, @p)", conn);
                cmd.Parameters.AddWithValue("@c", message);
                cmd.Parameters.AddWithValue("@d", DateTime.Now);
                cmd.Parameters.AddWithValue("@p", postedBy);
                cmd.ExecuteNonQuery();
            }
        }

        public bool AddNotice(NoticeModel notice)
        {
            AddNotice(notice.Content ?? "", notice.PostedBy ?? "Manager");
            return true;
        }

        public List<NoticeModel> GetAllNotices()
        {
            var list = new List<NoticeModel>();
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                var rdr = new SqliteCommand("SELECT * FROM Notices ORDER BY PostedDate DESC", conn).ExecuteReader();
                while (rdr.Read()) list.Add(new NoticeModel { Id = Convert.ToInt32(rdr["Id"]), Content = rdr["Content"].ToString() });
            }
            return list;
        }

        public bool DeleteNotice(int id)
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqliteCommand("DELETE FROM Notices WHERE Id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                return cmd.ExecuteNonQuery() > 0;
            }
        }
    }

    // --- MODELS ---
    public class SubTaskModel { public int SubTaskId { get; set; } public string? Description { get; set; } public bool IsCompleted { get; set; } }
    public class NoticeModel { public int Id { get; set; } public string? Content { get; set; } public string? Title { get; set; } public DateTime PostedDate { get; set; } public string? PostedBy { get; set; } }
}