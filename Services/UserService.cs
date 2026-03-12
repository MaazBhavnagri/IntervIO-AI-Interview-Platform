using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AIInterviewPractice.Models;

namespace AIInterviewPractice.Services
{
    public class UserService
    {
        private readonly string _dataFile = "Data/users.json";

        public UserService()
        {
            if (!Directory.Exists("Data")) Directory.CreateDirectory("Data");
            if (!File.Exists(_dataFile)) File.WriteAllText(_dataFile, "[]");
        }

        private List<User> ReadUsers()
        {
            var json = File.ReadAllText(_dataFile);
            return JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
        }

        private void WriteUsers(List<User> users)
        {
            var json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_dataFile, json);
        }

        public User CreateUser(string username, string email, string password)
        {
            var users = ReadUsers();
            if (users.Any(u => u.Username == username || u.Email == email)) return null;

            var user = new User
            {
                Username = username,
                Email = email,
                // Using requested simplest "hash" format for beginner project
                PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password))
            };

            users.Add(user);
            WriteUsers(users);
            return user;
        }

        public User ValidateLogin(string username, string password)
        {
            var users = ReadUsers();
            var hash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));
            return users.FirstOrDefault(u => u.Username == username && u.PasswordHash == hash);
        }

        public User GetUserById(string id)
        {
            return ReadUsers().FirstOrDefault(u => u.Id == id);
        }
    }
}
