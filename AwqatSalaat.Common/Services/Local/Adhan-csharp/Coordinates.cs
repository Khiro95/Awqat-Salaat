using System;

namespace Batoulapps.Adhan
{
    /// <summary>
    /// Coordinates representing a particular place
    /// </summary>
    public class Coordinates 
    {
        /// <summary>
        /// Latitude
        /// </summary>
        public readonly double Latitude;

        /// <summary>
        /// Longitude
        /// </summary>
        public readonly double Longitude;

        /// <summary>
        /// Generate a new Coordinates object
        /// </summary>
        /// <param name="latitude">the latitude of the place</param>
        /// <param name="longitude">the longitude of the place</param>
        public Coordinates(double latitude, double longitude) 
        {
            this.Latitude = latitude;
            this.Longitude = longitude;
        }
    }
}