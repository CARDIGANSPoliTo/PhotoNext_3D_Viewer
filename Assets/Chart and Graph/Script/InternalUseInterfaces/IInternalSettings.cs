using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChartAndGraph
{
    public interface IInternalSettings
    {
        event EventHandler InternalOnDataUpdate;
        event EventHandler InternalOnDataChanged;
    }
}
