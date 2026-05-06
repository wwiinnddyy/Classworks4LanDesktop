using System;

namespace ClassworksPlugin
{
    /// <summary>
    /// Simple data model representing a single assignment returned from
    /// the Classworks service.  Feel free to extend this class with
    /// additional properties as needed, for example a due date, status
    /// or course name.
    /// </summary>
    public sealed class Assignment
    {
        /// <summary>
        /// Gets or sets the title of the assignment.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the assignment.  The plugin
        /// displays this text beneath the title in the widget.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the due date of the assignment.  You can bind
        /// to this property in your XAML to display a formatted date.
        /// </summary>
        public DateTime? DueDate { get; set; }
    }
}