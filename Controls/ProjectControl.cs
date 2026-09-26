using System;
using System.ComponentModel;
using System.Windows;
using workspace_hub.Models;
using MediaBrush = System.Windows.Media.Brush;

namespace workspace_hub.Controls
{
    public class ProjectControl : System.Windows.Controls.UserControl
    {
        private Project? _observedProject;
        public ProjectControl()
        {
            Loaded += (_, _) => { ObserveProject(); UpdateDeadlineStateDisplay(); };
            Unloaded += (_, _) => StopObserving();
        }

        public static readonly DependencyProperty ProjectProperty =
            DependencyProperty.Register(nameof(Project), typeof(Project), typeof(ProjectControl),
                new PropertyMetadata(null, OnProjectChanged));

        public Project? Project
        {
            get => (Project?)GetValue(ProjectProperty);
            set => SetValue(ProjectProperty, value);
        }

        private static void OnProjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ProjectControl)d;
            control.StopObserving();
            control.DataContext = e.NewValue;
            if (control.IsLoaded) control.ObserveProject();
            control.UpdateDeadlineStateDisplay();
        }

        private void ObserveProject()
        {
            StopObserving();
            _observedProject = Project;
            if (_observedProject != null) _observedProject.PropertyChanged += Project_PropertyChanged;
        }

        private void StopObserving()
        {
            if (_observedProject != null) _observedProject.PropertyChanged -= Project_PropertyChanged;
            _observedProject = null;
        }

        private void Project_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Project.Deadline) || e.PropertyName == nameof(Project.Status))
                UpdateDeadlineStateDisplay();
        }

        public event EventHandler<Project>? OpenRequested;
        public event EventHandler<Project>? EditRequested;
        public event EventHandler<Project>? DeleteRequested;
        public event EventHandler<Project>? ProjectChanged;
        public event EventHandler<Project>? DetailsRequested;
        protected void RequestOpen() { if (Project != null) OpenRequested?.Invoke(this, Project); }
        protected void RequestEdit() { if (Project != null) EditRequested?.Invoke(this, Project); }
        protected void RequestDelete() { if (Project != null) DeleteRequested?.Invoke(this, Project); }
        protected void NotifyChanged() { if (Project != null) ProjectChanged?.Invoke(this, Project); }
        protected void RequestDetails() { if (Project != null) DetailsRequested?.Invoke(this, Project); }
        protected void UpdateDeadlineStateDisplay()
        {
            // Find the DeadlineStateText TextBlock by name
            var tb = this.FindName("DeadlineStateText") as System.Windows.Controls.TextBlock;
            if (tb == null) return;

            if (Project == null || Project.Deadline == null)
            {
                tb.Text = string.Empty;
                tb.Foreground = (MediaBrush)FindResource("DeadlineNormalBrush");
                return;
            }

            var now = DateTime.Now.Date;
            var deadline = Project.Deadline.Value.Date;

            // If completed, show Completed
            if (string.Equals(Project.Status ?? string.Empty, "Done", StringComparison.OrdinalIgnoreCase))
            {
                tb.Text = "Completed";
                tb.Foreground = (MediaBrush)FindResource("DeadlineCompletedBrush");
                return;
            }

            if (deadline < now)
            {
                var days = (now - deadline).Days;
                tb.Text = days == 1 ? "Overdue by 1 day" : $"Overdue by {days} days";
                tb.Foreground = (MediaBrush)FindResource("DeadlineOverdueBrush");
            }
            else if (deadline == now)
            {
                tb.Text = "Due today";
                tb.Foreground = (MediaBrush)FindResource("DeadlineSoonBrush");
            }
            else if (deadline == now.AddDays(1))
            {
                tb.Text = "Due tomorrow";
                tb.Foreground = (MediaBrush)FindResource("DeadlineSoonBrush");
            }
            else if (deadline <= now.AddDays(3))
            {
                var days = (deadline - now).Days;
                tb.Text = days == 1 ? "Due in 1 day" : $"Due in {days} days";
                tb.Foreground = (MediaBrush)FindResource("DeadlineSoonBrush");
            }
            else
            {
                tb.Text = string.Empty;
                tb.Foreground = (MediaBrush)FindResource("DeadlineNormalBrush");
            }
        }
    }
}
