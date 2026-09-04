using Microsoft.AspNetCore.Mvc.Razor;

namespace MyCampus.Data
{
    public class CustomViewLocationExpander : IViewLocationExpander
    {
        public void PopulateValues(ViewLocationExpanderContext context)
        {
        }

        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            var locations = viewLocations.ToList();
            var controller = context.ControllerName;

            if (!string.IsNullOrEmpty(controller))
            {
                // Support singular versions of controller names for views (e.g., Rooms -> Room)
                if (controller.EndsWith("s", StringComparison.OrdinalIgnoreCase))
                {
                    var singular = controller.Substring(0, controller.Length - 1);
                    locations.Add($"/Views/{singular}/{{0}}.cshtml");
                }

                if (controller.Equals("CampusEvents", StringComparison.OrdinalIgnoreCase) ||
                    controller.Equals("Events", StringComparison.OrdinalIgnoreCase))
                {
                    locations.Add("/Views/CampusEvent/{0}.cshtml");
                    locations.Add("/Views/Events/{0}.cshtml");
                    locations.Add("/Views/Event/{0}.cshtml");
                }

                if (controller.Equals("RoomBookings", StringComparison.OrdinalIgnoreCase) ||
                    controller.Equals("RoomBooking", StringComparison.OrdinalIgnoreCase))
                {
                    locations.Add("/Views/RoomBooking/{0}.cshtml");
                    locations.Add("/Views/RoomBookings/{0}.cshtml");
                }
            }

            return locations;
        }
    }
}
