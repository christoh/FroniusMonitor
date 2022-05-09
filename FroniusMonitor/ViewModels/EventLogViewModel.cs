using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using De.Hochstaetter.Fronius.Contracts;
using De.Hochstaetter.Fronius.Extensions;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.Gen24;
using De.Hochstaetter.Fronius.Services;

namespace De.Hochstaetter.FroniusMonitor.ViewModels
{
    public class EventLogViewModel:ViewModelBase
    {
        private readonly IWebClientService webClientService;

        public EventLogViewModel(IWebClientService webClientService)
        {
            this.webClientService = webClientService;
        }

        private IOrderedEnumerable<Gen24Event>? events;

        public IOrderedEnumerable<Gen24Event>? Events
        {
            get => events;
            set => Set(ref events, value);
        }

        internal override async Task OnInitialize()
        {
            await base.OnInitialize().ConfigureAwait(false);
            Events = await webClientService.GetFroniusEvents().ConfigureAwait(false);
        }
    }
}
