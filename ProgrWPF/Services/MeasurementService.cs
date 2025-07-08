using ProgrWPF.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ProgrWPF.Services
{
    public class MeasurementService
    {
        public event Action<MeasurementResult> PointMeasurementStarted;
        public event Action<(MeasurementResult result, double progress)> PointMeasurementProgress;
        public event Action<MeasurementResult> PointMeasurementCompleted;
        public event Action AllMeasurementsCompleted;
        public event Action<List<MeasurementResult>> MeasurementReset;


        private DispatcherTimer simulationTimer;
        private List<MeasurementResult> measurementQueue;
        private int currentPointIndex;
        private readonly Random random = new Random();

        public MeasurementService()
        {
            simulationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            simulationTimer.Tick += SimulationTimer_Tick;
        }

        public void StartMeasurement(IEnumerable<MeasurementResult> results)
        {
            measurementQueue = new List<MeasurementResult>(results);
            currentPointIndex = 0;
            
            // Reset status for all points before starting
            foreach (var result in measurementQueue)
            {
                result.Status = MeasurementStatus.NotMeasured;
            }
            MeasurementReset?.Invoke(measurementQueue);

            if (measurementQueue.Count > 0)
            {
                simulationTimer.Start();
            }
        }

        public void PauseMeasurement()
        {
            simulationTimer.Stop();
        }

        public void StopMeasurement()
        {
            simulationTimer.Stop();
            currentPointIndex = 0;
            if (measurementQueue != null)
            {
                foreach (var result in measurementQueue)
                {
                    result.Status = MeasurementStatus.NotMeasured;
                }
                MeasurementReset?.Invoke(measurementQueue);
            }
        }

        public void RepeatMeasurement(MeasurementResult result)
        {
            if (result != null)
            {
                MeasurePoint(result);
            }
        }

        private void SimulationTimer_Tick(object sender, EventArgs e)
        {
            if (currentPointIndex >= measurementQueue.Count)
            {
                simulationTimer.Stop();
                AllMeasurementsCompleted?.Invoke();
                return;
            }

            var result = measurementQueue[currentPointIndex];
            MeasurePoint(result);

            currentPointIndex++;
        }

        private async void MeasurePoint(MeasurementResult result)
        {
            result.Status = MeasurementStatus.InProgress;
            PointMeasurementStarted?.Invoke(result);
            PointMeasurementProgress?.Invoke((result, 0));

            // Simulate measurement work with progress updates
            for (int i = 1; i <= 10; i++)
            {
                await Task.Delay(100); // Simulate a step of work
                PointMeasurementProgress?.Invoke((result, i * 10));
            }

            // Simulate measurement result
            result.ActualX = result.ExpectedX + (random.NextDouble() - 0.5) * 0.2;
            result.ActualY = result.ExpectedY + (random.NextDouble() - 0.5) * 0.2;
            result.ActualZ = result.ExpectedZ + (random.NextDouble() - 0.5) * 0.2;
            result.Timestamp = DateTime.Now;

            result.Deviation = Math.Sqrt(
                Math.Pow(result.ActualX - result.ExpectedX, 2) +
                Math.Pow(result.ActualY - result.ExpectedY, 2) +
                Math.Pow(result.ActualZ - result.ExpectedZ, 2)
            );

            result.Status = result.Deviation <= result.Tolerance ? MeasurementStatus.Passed : MeasurementStatus.Failed;
            PointMeasurementCompleted?.Invoke(result);
        }
    }
}
