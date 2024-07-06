using System;
using System.Collections.Generic;

namespace PvpStats.Types;
internal class GamePeriod {

    public static readonly Dictionary<int, GamePeriod> Expansion = new() {
        { 6, new GamePeriod() {
            Name = "Endwalker",
            StartDate = new DateTime(2021,12,11,11,0,0,DateTimeKind.Utc),
            EndDate = new DateTime(2024,6,26,11,0,0,DateTimeKind.Utc),
        } },
        { 7, new GamePeriod() {
            Name = "Dawntrail",
            StartDate = new DateTime(2024,6,26,11,0,0,DateTimeKind.Utc),
            EndDate = new DateTime(2028,6,26,11,0,0,DateTimeKind.Utc),
        } },
    };

    public static readonly Dictionary<int, GamePeriod> Season = new() {
        { 1, new GamePeriod() {
            Name = "1",
            StartDate = new DateTime(2022,4,11),
            EndDate = new DateTime(2022,7,4),
        } },
        { 2, new GamePeriod() {
            Name = "2",
            StartDate = new DateTime(2022,7,4),
            EndDate = new DateTime(2022,8,23),
        } },
        { 3, new GamePeriod() {
            Name = "3",
            StartDate = new DateTime(2022,8,23),
            EndDate = new DateTime(2022,11,1),
        } },
        { 4, new GamePeriod() {
            Name = "4",
            StartDate = new DateTime(2022,11,1),
            EndDate = new DateTime(2023,1,10),
        } },
        { 5, new GamePeriod() {
            Name = "5",
            StartDate = new DateTime(2023,1,10),
            EndDate = new DateTime(2023,4,3),
        } },
        { 6, new GamePeriod() {
            Name = "6",
            StartDate = new DateTime(2023,4,3),
            EndDate = new DateTime(2023,5,23),
        } },
        { 7, new GamePeriod() {
            Name = "7",
            StartDate = new DateTime(2023,5,23),
            EndDate = new DateTime(2023,8,8),
        } },
        { 8, new GamePeriod() {
            Name = "8",
            StartDate = new DateTime(2023,8,8),
            EndDate = new DateTime(2023,10,31),
        } },
        { 9, new GamePeriod() {
            Name = "9",
            StartDate = new DateTime(2023,10,31),
            EndDate = new DateTime(2024,1,16),
        } },
        { 10, new GamePeriod() {
            Name = "10",
            StartDate = new DateTime(2024,1,16),
            EndDate = new DateTime(2024,3,19,1,0,0,DateTimeKind.Utc),
        } },
        { 11, new GamePeriod() {
            Name = "11",
            StartDate = new DateTime(2024,3,19,10,0,0,DateTimeKind.Utc),
            EndDate = new DateTime(2024,6,26,11,0,0,DateTimeKind.Utc),
        } },
    };

    public string Name { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
