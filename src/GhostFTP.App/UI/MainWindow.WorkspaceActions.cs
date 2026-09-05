using GhostFTP.Core.Models;
using System.Windows;

namespace GhostFTP.UI;

public sealed partial class MainWindow
{
    private void AppendConnectionLog(string message, string level = "INFO")
    {
        var normalizedLevel = string.IsNullOrWhiteSpace(level) ? "INFO" : level.Trim().ToUpperInvariant();
        _connectionLog.Add($"{DateTime.Now:HH:mm:ss}  [{normalizedLevel}]  {message}");
        while (_connectionLog.Count > 400)
            _connectionLog.RemoveAt(0);

        if (_connectionLog.Count > 0)
            _connectionLogList.ScrollIntoView(_connectionLog[^1]);
    }

    private async Task OpenSiteManagerAsync()
    {
        if (_profileStore is null)
            return;

        var selectedId = (_profilesList.SelectedItem as ServerProfile)?.Id;
        var dialog = new SiteManagerDialog(
            this,
            _profiles,
            profile => profile.IsDemo ? string.Empty : _profileStore.GetPassword(profile));

        if (dialog.ShowDialog() != true)
            return;

        var nextProfiles = dialog.Profiles.Select(x => x.Clone()).ToArray();
        _profiles.Clear();
        foreach (var profile in nextProfiles)
        {
            if (!profile.IsDemo && dialog.Passwords.TryGetValue(profile.Id, out var password))
                _profileStore.SetPassword(profile, password);
            _profiles.Add(profile);
        }

        await SaveProfilesSafeAsync();

        var targetId = dialog.ConnectProfileId ?? selectedId;
        if (targetId is Guid id)
        {
            var selected = _profiles.FirstOrDefault(x => x.Id == id);
            if (selected is not null)
            {
                _profilesList.SelectedItem = selected;
                ProfileSelected();
            }
        }
        else if (_profiles.Count > 0)
        {
            _profilesList.SelectedIndex = 0;
        }

        AppendConnectionLog("Saved Site Manager changes locally.", "INFO");

        if (dialog.ConnectProfileId is not null)
            await ConnectAsync();
    }
}
