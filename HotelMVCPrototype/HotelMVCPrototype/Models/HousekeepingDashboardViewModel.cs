namespace HotelMVCPrototype.Models
{
    public class HousekeepingDashboardViewModel
    {
        public List<Room> Rooms { get; set; }

        public RoomStatisticsViewModel RoomStatistics { get; set; }

        public RoomMapPageViewModel RoomMapPage { get; set; }

        public List<CleaningLog> TodaysCleanings { get; set; } = new();

        public List<RoomIssue> OpenHousekeepingIssues { get; set; } = new();

    }
}
