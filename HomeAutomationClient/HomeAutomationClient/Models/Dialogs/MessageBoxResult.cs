using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.HomeAutomationClient.Models.Dialogs
{
    public class MessageBoxResult
    {
        public int Index { get; init; } = ~0;
        public string? ButtonText { get; init; }
    }
}
