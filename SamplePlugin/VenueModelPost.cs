using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venyou
{
    public class VenueModelPost
    {
        public bool status = false;
        public string owner = string.Empty;
        public string opening_times = string.Empty;
        public string name = string.Empty;
        public string location = string.Empty;
        public string description = string.Empty;
    }

    public class VenueModelResponse
    {
        public int? id;
        public bool? status;
        public string? opening_times;
        public string? name;
        public string? location;
        public string? description;
    }
}
