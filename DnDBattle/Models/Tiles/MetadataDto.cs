using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnDBattle.Models.Tiles
{
    /// <summary>
    /// Data Transfer Object for serializing tile metadata
    /// Wraps any metadata type into a generic container
    /// </summary>
    public class MetadataDto
    {
        /// <summary>
        /// The type of metadata (Trap, Secret, Interactive, etc.)
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The serialized JSON data of the metadata object
        /// </summary>
        public string Data { get; set; }
    }
}
