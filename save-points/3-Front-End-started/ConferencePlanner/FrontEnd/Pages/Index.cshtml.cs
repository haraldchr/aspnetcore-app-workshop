﻿using ConferenceDTO;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Pages
{
    public class IndexModel : PageModel
    {
        public IEnumerable<IGrouping<DateTimeOffset?, SessionResponse>> Sessions { get; set; }

        public IEnumerable<(int Offset, DayOfWeek? DayofWeek)> DayOffsets { get; set; }

        public int CurrentDayOffset { get; set; }

        protected readonly IApiClient _apiClient;
        private readonly IAuthorizationService _authorizationService;

        public bool IsAdmin { get; set; }

        public List<int> UserSessions { get; set; }

        public IndexModel(IApiClient apiClient, IAuthorizationService authorizationService)
        {
            _apiClient = apiClient;
            _authorizationService = authorizationService;
        }

        public async Task OnGet(int day = 0)
        {
            var authorizationResult = await _authorizationService.AuthorizeAsync(User, "Admin");
            IsAdmin = authorizationResult.Succeeded;

            var userSessions = await _apiClient.GetSessionsByAttendeeAsync(User.Identity.Name);

            UserSessions = userSessions.Select(u => u.ID).ToList();

            CurrentDayOffset = day;

            var sessions = await GetSessionsAsync();

            var startDate = sessions.Min(s => s.StartTime?.Date);
            var endDate = sessions.Max(s => s.EndTime?.Date);

            var numberOfDays = ((endDate - startDate)?.Days) + 1;

            DayOffsets = Enumerable.Range(0, numberOfDays ?? 0)
                .Select(offset => (offset, (startDate?.AddDays(offset))?.DayOfWeek));

            var filterDate = startDate?.AddDays(day);

            Sessions = sessions.Where(s => s.StartTime?.Date == filterDate)
                               .OrderBy(s => s.TrackId)
                               .GroupBy(s => s.StartTime)
                               .OrderBy(g => g.Key);
        }

        protected virtual Task<List<SessionResponse>> GetSessionsAsync()
        {
            return _apiClient.GetSessionsAsync();
        }

        public async Task<IActionResult> OnPostAsync(int sessionId)
        {
            await _apiClient.AddSessionToAttendeeAsync(User.Identity.Name, sessionId);

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemoveAsync(int sessionId)
        {
            await _apiClient.RemoveSessionFromAttendeeAsync(User.Identity.Name, sessionId);

            return RedirectToPage();
        }
    }
}
