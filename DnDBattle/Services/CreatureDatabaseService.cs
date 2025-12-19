using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;
using System.Threading.Tasks;
using System.IO;
using DnDBattle.Models;

namespace DnDBattle.Services
{
    public static class CreatureDatabaseService
    {
        private const string DatabasePath = "Creatures.db";
        private const string ConnectionString = $"Data Source={DatabasePath};Version=3";

        public static void InitializeDatabase()
        {
            if (!File.Exists(DatabasePath))
            {
                SQLiteConnection.CreateFile(DatabasePath);
                using var conn = new SQLiteConnection(ConnectionString);
                conn.Open();

                    var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                CREATE TABLE Creatures (
                    Id TEXT PRIMARY KEY,
                    Name TEXT,
                    Type TEXT,
                    Size TEXT,
                    Alignment TEXT,
                    ChallengeRating TEXT,
                    ArmorClass INTEGER,
                    MaxHP INTEGER,
                    HitDice TEXT,
                    Speed TEXT,
                    Tags TEXT
                );
                ";
                cmd.ExecuteNonQuery();
            }
        }

        public static List<Token> LoadCreatures()
        {
            var creatures = new List<Token>();
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Creatures;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var creature = new Token()
                {
                    Id = Guid.Parse(reader["Id"].ToString()),
                    Name = reader["Name"].ToString(),
                    Type = reader["Type"].ToString(),
                    Size = reader["Size"].ToString(),
                    Alignment = reader["Alignment"].ToString(),
                    ChallengeRating = reader["ChallengeRating"].ToString(),
                    ArmorClass = Convert.ToInt32(reader["ArmorClass"]),
                    MaxHP = Convert.ToInt32(reader["MaxHP"]),
                    HitDice = reader["HitDice"].ToString(),
                    Speed = reader["Speed"].ToString(),
                    Tags = new List<string>(reader["Tags"].ToString().Split(","))
                };
                creatures.Add(creature);
            }
            return creatures;
        }

        public static void SaveCreature(IEnumerable<Token> creatures)
        {
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Creatures";
            cmd.ExecuteNonQuery();

            foreach (var creature in creatures)
            {
                cmd.CommandText = @"
                INSERT INTO Creatures (Id, Name, Type, Size, Alignment, ChallengeRating, ArmorClass, MaxHP, HitDice, Speed, Tags)
                VALUES (@Id, @Name, @Type, @Size, @Alignment, @ChallengeRating, @ArmorClass, @MaxHP, @HitDice, @Speed, @Tags);";
                cmd.Parameters.AddWithValue("@Id", creature.Id.ToString());
                cmd.Parameters.AddWithValue("@Name", creature.Name);
                cmd.Parameters.AddWithValue("@Type", creature.Type);
                cmd.Parameters.AddWithValue("@Size", creature.Size);
                cmd.Parameters.AddWithValue("@Alignment", creature.Alignment);
                cmd.Parameters.AddWithValue("@ChallengeRating", creature.ChallengeRating);
                cmd.Parameters.AddWithValue("@ArmorClass", creature.ArmorClass);
                cmd.Parameters.AddWithValue("@MaxHP", creature.MaxHP);
                cmd.Parameters.AddWithValue("@HitDice", creature.HitDice);
                cmd.Parameters.AddWithValue("@Speed", creature.Speed);
                cmd.Parameters.AddWithValue("@Tags", string.Join(",", creature.Tags));
                cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
            }
        }
    }
}
