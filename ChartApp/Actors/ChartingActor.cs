﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Akka.Actor;

namespace ChartApp.Actors
{
    public class ChartingActor : ReceiveActor, IWithUnboundedStash
    {
        public const int MaxPoints = 250;
        private int xPosCounter = 0;
        
        #region Messages

        public class AddSeries
        {
            public Series Series { get; }

            public AddSeries(Series series)
            {
                Series = series;
            }
        }

        public class InitializeChart
        {
            public InitializeChart(Dictionary<string, Series> initialSeries)
            {
                InitialSeries = initialSeries;
            }

            public Dictionary<string, Series> InitialSeries { get; private set; }
        }
        
        public class RemoveSeries
        {
            public RemoveSeries(string seriesName)
            {
                SeriesName = seriesName;
            }

            public string SeriesName { get; }

            public bool NameExistsIn(Dictionary<string,Series> index) => !string.IsNullOrEmpty(SeriesName) && index.ContainsKey(SeriesName);
        }

        public class TogglePause { }

        #endregion

        private readonly Chart _chart;
        private Dictionary<string, Series> _seriesIndex;
        private readonly Button _pauseButton;
        
        public IStash Stash { get; set; }

        public ChartingActor(Chart chart, Button pauseButton) : this(chart, new Dictionary<string, Series>(), pauseButton)
        {
        }

        public ChartingActor(Chart chart, Dictionary<string, Series> seriesIndex, Button pauseButton)
        {
            _chart = chart;
            _seriesIndex = seriesIndex;
            _pauseButton = pauseButton;

            Receive<InitializeChart>(HandleInitialize);
            Receive<AddSeries>(HandleAddSeries);
            Receive<RemoveSeries>(HandleRemoveSeries);
            Receive<Metric>(HandleMetrics);
            Receive<TogglePause>(pause =>
            {
                SetPauseButtonText(true);
                BecomeStacked(Paused);
            });
        }
        
        private void Paused()
        {
            Receive<AddSeries>(addSeries => Stash.Stash());
            Receive<RemoveSeries>(removeSeries => Stash.Stash());
            Receive<Metric>(HandleMetricsPaused);
            Receive<TogglePause>(pause =>
            {
                SetPauseButtonText(false);
                UnbecomeStacked();
                Stash.UnstashAll();
            });
        }

        private void HandleAddSeries(AddSeries series)
        {
            var seriesName = series.Series.Name;
            if (string.IsNullOrEmpty(seriesName) || _seriesIndex.ContainsKey(seriesName)) return;

            _seriesIndex.Add(seriesName, series.Series);
            _chart.Series.Add(series.Series);
            SetChartBoundaries();
        }
        
        #region Individual Message Type Handlers

        private void HandleInitialize(InitializeChart ic)
        {
            if (ic.InitialSeries != null)
            {
                //swap the two series out
                _seriesIndex = ic.InitialSeries;
            }

            //delete any existing series
            _chart.Series.Clear();
            
            // set the axes up
            var area = _chart.ChartAreas[0];
            area.AxisX.IntervalType = DateTimeIntervalType.Number;
            area.AxisY.IntervalType = DateTimeIntervalType.Number;

            SetChartBoundaries();

            //attempt to render the initial chart
            if (_seriesIndex.Any())
            {
                foreach (var series in _seriesIndex)
                {
                    //force both the chart and the internal index to use the same names
                    series.Value.Name = series.Key;
                    _chart.Series.Add(series.Value);
                }
            }
            
            SetChartBoundaries();
        }
        
        private void HandleRemoveSeries(RemoveSeries series)
        {
            if (series.NameExistsIn(_seriesIndex))
            {
                var seriesToRemove = _seriesIndex[series.SeriesName];
                _seriesIndex.Remove(series.SeriesName);
                _chart.Series.Remove(seriesToRemove);
                SetChartBoundaries();
            }
        }

        private void HandleMetrics(Metric metric)
        {
            if (metric.SeriesExistsIn(_seriesIndex))
            {
                var series = _seriesIndex[metric.Series];
                series.Points.AddXY(xPosCounter++, metric.CounterValue);
                while(series.Points.Count > MaxPoints) series.Points.RemoveAt(0);
                SetChartBoundaries();
            }
        }
        
        private void HandleMetricsPaused(Metric metric)
        {
            if (metric.SeriesExistsIn(_seriesIndex))
            {
                var series = _seriesIndex[metric.Series];
                if (series.Points == null) return; // means we're shutting down
                series.Points.AddXY(xPosCounter++, 0.0d); //set the Y value to zero when we're paused
                while (series.Points.Count > MaxPoints) series.Points.RemoveAt(0);
                SetChartBoundaries();
            }
        }

        #endregion
        
        private void SetChartBoundaries()
        {
            double maxAxisX, maxAxisY, minAxisX, minAxisY = 0.0d;
            var allPoints = _seriesIndex.Values.SelectMany(series => series.Points).ToList();
            var yValues = allPoints.SelectMany(point => point.YValues).ToList();
            maxAxisX = xPosCounter;
            minAxisX = xPosCounter - MaxPoints;
            maxAxisY = yValues.Count > 0 ? Math.Ceiling(yValues.Max()) : 1.0d;
            minAxisY = yValues.Count > 0 ? Math.Floor(yValues.Min()) : 0.0d;
            if (allPoints.Count > 2)
            {
                var area = _chart.ChartAreas[0];
                area.AxisX.Minimum = minAxisX;
                area.AxisX.Maximum = maxAxisX;
                area.AxisY.Minimum = minAxisY;
                area.AxisY.Maximum = maxAxisY;
            }
        }
        
        private void SetPauseButtonText(bool paused)
        {
            _pauseButton.Text = $"{(!paused ? "PAUSE ||" : "RESUME ->")}";
        }
    }
}
