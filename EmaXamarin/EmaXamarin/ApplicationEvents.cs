using System;

namespace EmaXamarin
{
    public class ApplicationEvents
    {
        public event EventHandler Resumed;

        public void OnResumed()
        {
            if (Resumed != null)
                Resumed.Invoke(this, EventArgs.Empty);
        }

    }
}