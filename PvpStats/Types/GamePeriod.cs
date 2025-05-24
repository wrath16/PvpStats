using System;
using System.Collections.Generic;

namespace PvpStats.Types;
internal class GamePeriod {

    public static readonly List<GamePeriod> Expansion = new() {
        { new GamePeriod() {
            Name = "Endwalker",
            StartDate = new DateTime(2021,12,11,11,0,0,DateTimeKind.Utc),
            EndDate = new DateTime(2024,6,26,11,0,0,DateTimeKind.Utc),
        } },
        { new GamePeriod() {
            Name = "Dawntrail",
            StartDate = new DateTime(2024,6,26,11,0,0,DateTimeKind.Utc),
            EndDate = new DateTime(2028,6,26,11,0,0,DateTimeKind.Utc),
        } },
    };

    public static readonly List<GamePeriod> Season = new() {
        { new GamePeriod() {
            Name = "1",
            StartDate = new DateTime(2022,4,11),
            EndDate = new DateTime(2022,7,4),
        } },
        { new GamePeriod() {
            Name = "2",
            StartDate = new DateTime(2022,7,4),
            EndDate = new DateTime(2022,8,23),
        } },
        { new GamePeriod() {
            Name = "3",
            StartDate = new DateTime(2022,8,23),
            EndDate = new DateTime(2022,11,1),
        } },
        { new GamePeriod() {
            Name = "4",
            StartDate = new DateTime(2022,11,1),
            EndDate = new DateTime(2023,1,10),
        } },
        { new GamePeriod() {
            Name = "5",
            StartDate = new DateTime(2023,1,10),
            EndDate = new DateTime(2023,4,3),
        } },
        { new GamePeriod() {
            Name = "6",
            StartDate = new DateTime(2023,4,3),
            EndDate = new DateTime(2023,5,23),
        } },
        { new GamePeriod() {
            Name = "7",
            StartDate = new DateTime(2023,5,23),
            EndDate = new DateTime(2023,8,8),
        } },
        { new GamePeriod() {
            Name = "8",
            StartDate = new DateTime(2023,8,8),
            EndDate = new DateTime(2023,10,31),
        } },
        { new GamePeriod() {
            Name = "9",
            StartDate = new DateTime(2023,10,31),
            EndDate = new DateTime(2024,1,16),
        } },
        { new GamePeriod() {
            Name = "10",
            StartDate = new DateTime(2024,1,16),
            EndDate = new DateTime(2024,3,19,1,0,0,DateTimeKind.Utc),
        } },
        { new GamePeriod() {
            Name = "11",
            StartDate = new DateTime(2024,3,19,10,0,0,DateTimeKind.Utc),
            EndDate = new DateTime(2024,6,26,11,0,0,DateTimeKind.Utc),
        } },
        { new GamePeriod() {
            Name = "12",
            StartDate = new DateTime(2024,11,12,10,0,0,DateTimeKind.Utc),
            EndDate = new DateTime(2025,1,21,10,0,0,DateTimeKind.Utc),
        } },
        { new GamePeriod() {
            Name = "13",
            StartDate = new DateTime(2025,1,21,10,0,0,DateTimeKind.Utc),
            EndDate = new DateTime(2025,3,25,10,0,0,DateTimeKind.Utc),
        } },
        { new GamePeriod() {
            Name = "14",
            StartDate = new DateTime(2025,3,25,10,0,0,DateTimeKind.Utc),
            EndDate = new DateTime(2025,5,27,10,0,0,DateTimeKind.Utc),
        } },
        { new GamePeriod() {
            Name = "15",
            StartDate = new DateTime(2025,5,27,10,0,0,DateTimeKind.Utc),
        } },
    };

    public static readonly List<GamePeriod> Patch = new() {
        { new GamePeriod() {
            Name = "7.1X",
            StartDate = new DateTime(2024,11,12,10,0,0,DateTimeKind.Utc),
            EndDate = new DateTime(2025,3,25,10,0,0,DateTimeKind.Utc),
        } },
        { new GamePeriod() {
            Name = "7.1",
            StartDate = new DateTime(2024,11,12,10,0,0,DateTimeKind.Utc),
            EndDate = new DateTime(2024,11,26,10,0,0,DateTimeKind.Utc),
        } },
        { new GamePeriod() {
            Name = "7.11",
            StartDate = new DateTime(2024,11,26,10,0,0,DateTimeKind.Utc),
            EndDate = new DateTime(2024,12,17,10,0,0,DateTimeKind.Utc),
        } },
        { new GamePeriod() {
            Name = "7.15",
            StartDate = new DateTime(2024,12,17,10,0,0,DateTimeKind.Utc),
            EndDate = new DateTime(2025,1,21,10,0,0,DateTimeKind.Utc),
        } },
        { new GamePeriod() {
            Name = "7.16",
            StartDate = new DateTime(2025,1,21,10,0,0,DateTimeKind.Utc),
            EndDate = new DateTime(2025,2,25,10,0,0,DateTimeKind.Utc),
        } },
        { new GamePeriod() {
            Name = "7.18",
            StartDate = new DateTime(2025,2,25,10,0,0,DateTimeKind.Utc),
            EndDate = new DateTime(2025,3,25,10,0,0,DateTimeKind.Utc),
        } },
        { new GamePeriod() {
            Name = "7.2X",
            StartDate = new DateTime(2025,3,25,10,0,0,DateTimeKind.Utc),
        } },
        { new GamePeriod() {
            Name = "7.2-7.25",
            StartDate = new DateTime(2025,3,25,10,0,0,DateTimeKind.Utc),
            EndDate = new DateTime(2025,5,27,10,0,0,DateTimeKind.Utc),
        } },
        { new GamePeriod() {
            Name = "7.28",
            StartDate = new DateTime(2025,5,27,10,0,0,DateTimeKind.Utc),
        } },
    };

    public string Name { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
