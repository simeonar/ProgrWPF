using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProgrWPF.Data
{
    public enum MeasurementStatus
    {
        NotMeasured,
        InProgress,
        Passed,
        Failed
    }

    public class MeasurementResult : INotifyPropertyChanged
    {
        private string _pointName;
        private MeasurementStatus _status;
        private double _deviation;
        private DateTime _timestamp;

        public string PointName
        {
            get => _pointName;
            set { _pointName = value; OnPropertyChanged(); }
        }

        public MeasurementStatus Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public double Deviation
        {
            get => _deviation;
            set { _deviation = value; OnPropertyChanged(); OnPropertyChanged(nameof(DeviationText)); }
        }

        public DateTime Timestamp
        {
            get => _timestamp;
            set { _timestamp = value; OnPropertyChanged(); OnPropertyChanged(nameof(TimestampText)); }
        }

        public double ExpectedX { get; set; }
        public double ExpectedY { get; set; }
        public double ExpectedZ { get; set; }
        public double ActualX { get; set; }
        public double ActualY { get; set; }
        public double ActualZ { get; set; }
        public double Tolerance { get; set; }

        // Properties for TreeView display
        public ObservableCollection<PropertyItem> Details { get; set; }

        // Formatted text for binding
        public string DeviationText => $"Deviation: {Deviation:F4}";
        public string ToleranceText => $"Tolerance: {Tolerance:F2}";
        public string TimestampText => $"Timestamp: {Timestamp:HH:mm:ss}";

        public MeasurementResult()
        {
            Details = new ObservableCollection<PropertyItem>();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void UpdateDetails()
        {
            Details.Clear();
            Details.Add(new PropertyItem { Name = DeviationText });
            Details.Add(new PropertyItem { Name = ToleranceText });
            Details.Add(new PropertyItem { Name = TimestampText });
        }
    }
}
