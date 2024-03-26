using System;
using System.Collections.Generic;

namespace PvpStats.Types.Match;
internal class ArenaSeason {

    public static readonly Dictionary<int, ArenaSeason> Season = new() {
        { 1, new ArenaSeason() {
            SeasonNumber = 1,
            StartDate = new DateTime(2022,4,11),
            EndDate = new DateTime(2022,7,4),
        } },
        { 2, new ArenaSeason() {
            SeasonNumber = 2,
            StartDate = new DateTime(2022,7,4),
            EndDate = new DateTime(2022,8,23),
        } },
        { 3, new ArenaSeason() {
            SeasonNumber = 3,
            StartDate = new DateTime(2022,8,23),
            EndDate = new DateTime(2022,11,1),
        } },
        { 4, new ArenaSeason() {
            SeasonNumber = 4,
            StartDate = new DateTime(2022,11,1),
            EndDate = new DateTime(2023,1,10),
        } },
        { 5, new ArenaSeason() {
            SeasonNumber = 5,
            StartDate = new DateTime(2023,1,10),
            EndDate = new DateTime(2023,4,3),
        } },
        { 6, new ArenaSeason() {
            SeasonNumber = 6,
            StartDate = new DateTime(2023,4,3),
            EndDate = new DateTime(2023,5,23),
        } },
        { 7, new ArenaSeason() {
            SeasonNumber = 7,
            StartDate = new DateTime(2023,5,23),
            EndDate = new DateTime(2023,8,8),
        } },
        { 8, new ArenaSeason() {
            SeasonNumber = 8,
            StartDate = new DateTime(2023,8,8),
            EndDate = new DateTime(2023,10,31),
        } },
        { 9, new ArenaSeason() {
            SeasonNumber = 9,
            StartDate = new DateTime(2023,10,31),
            EndDate = new DateTime(2024,1,16),
        } },
        { 10, new ArenaSeason() {
            SeasonNumber = 10,
            StartDate = new DateTime(2024,1,16),
            EndDate = new DateTime(2024,3,19),
        } },
        { 11, new ArenaSeason() {
            SeasonNumber = 11,
            StartDate = new DateTime(2024,3,19),
            EndDate = new DateTime(2024,7,2),
        } },
    };

    public int SeasonNumber { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
