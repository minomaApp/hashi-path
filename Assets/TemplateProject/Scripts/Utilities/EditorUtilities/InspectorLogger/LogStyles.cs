using UnityEngine;

namespace TemplateProject.Scripts.Utilities.EditorUtilities.InspectorLogger
{
    public static class LogStyles
    {
        public static readonly LogStyle Positive = new LogStyle(
            color: new Color(0.2f, 1.0f, 0.2f),
            bold: true,
            size: 14
        );

        public static readonly LogStyle Negative = new LogStyle(
            color: new Color(1.0f, 0.2f, 0.2f),
            bold: true,
            italic: true,
            size: 14
        );

        public static readonly LogStyle Info = new LogStyle(
            color: new Color(0.6f, 0.6f, 1.0f),
            italic: true,
            size: 12
        );

        public static readonly LogStyle Positive2 = new LogStyle(
            color: new Color(0.2f, 0.6f, 1.0f),
            bold: true,
            size: 14
        );

        public static readonly LogStyle Negative2 = new LogStyle(
            color: new Color(1.0f, 0.4f, 0.2f),
            bold: true,
            italic: true,
            size: 14
        );

        public static readonly LogStyle Info2 = new LogStyle(
            color: new Color(0f, 1.0f, 0.5f),
            italic: true,
            size: 12
        );

        public static readonly LogStyle Positive3 = new LogStyle(
            color: new Color(0.9f, 0.8f, 0.0f),
            bold: true,
            size: 12
        );

        public static readonly LogStyle Negative3 = new LogStyle(
            color: new Color(0.9f, 0.2f, 0.4f),
            bold: true,
            italic: true,
            size: 12
        );

        public static readonly LogStyle Info3 = new LogStyle(
            color: new Color(0.2f, 0.8f, 1.0f),
            italic: true,
            size: 12
        );
    }
}
