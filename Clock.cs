using System;
using System.Collections.Generic;

using System.Text;
using System.Timers;

namespace TerraFirma
{
    class Clock
    {
        private TimeSpan m_InitialTime;
        private TimeSpan m_Time;

        internal TimeSpan Time { get { return m_Time; } set { m_InitialTime = value; m_Time = value; } }
        internal TimeSpan Incremental { get; set; }

        internal TimeSpan CurrentMoveTimeElapsed { get; set; }
        private Timer timer;
        private long minimalTimeTicks = TimeSpan.FromSeconds(10).Ticks;
        internal bool TimesUp { get { return CurrentMoveTimeElapsed.Ticks > minimalTimeTicks; } }


        internal Clock()
        {
            timer = new Timer();
            timer.Interval = 1000;
            timer.AutoReset = true;
            timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
        }
        internal void Start()
        {
            timer.Start();
            CurrentMoveTimeElapsed = TimeSpan.FromSeconds(0);
        }

        internal void Stop()
        {
            timer.Stop();
            CurrentMoveTimeElapsed = TimeSpan.FromSeconds(0);
        }
        internal void PlayerMoved()
        {
            Time.Add(Incremental);
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Time = Time.Subtract(TimeSpan.FromSeconds(1));
            CurrentMoveTimeElapsed = CurrentMoveTimeElapsed.Add(TimeSpan.FromSeconds(1));
        }


        internal void Reset()
        {
            m_Time = m_InitialTime;
        }
    }
}
