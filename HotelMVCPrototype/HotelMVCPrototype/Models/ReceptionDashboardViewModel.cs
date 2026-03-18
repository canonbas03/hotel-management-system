using HotelMVCPrototype.Models;
using System.Collections.Generic;

namespace HotelMVCPrototype.Models
{
    public class ReceptionDashboardViewModel
    {
        public List<Room> Rooms { get; set; }

        public RoomStatisticsViewModel RoomStatistics { get; set; }

        public List<RoomMapViewModel> RoomMap { get; set; }

        public int CurrentFloor { get; set; }
    }
}
