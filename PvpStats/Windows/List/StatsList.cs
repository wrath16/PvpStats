using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PvpStats.Helpers;
using PvpStats.Types.Display;
using PvpStats.Types.Match;
using PvpStats.Types.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace PvpStats.Windows.List;
internal abstract class StatsList<T, U, V> : FilteredList<T, V> where T : notnull where U : PlayerJobStats where V : PvpMatch {

    protected List<T> DataModelUntruncated { get; set; } = [];

    protected Dictionary<T, U> StatsModel { get; set; } = [];

    //protected List<V> Matches = new();

    protected override ImGuiTableFlags TableFlags { get; set; } = ImGuiTableFlags.Reorderable | ImGuiTableFlags.Sortable | ImGuiTableFlags.Hideable
    | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.ScrollX | ImGuiTableFlags.PadOuterX;
    protected override bool ShowHeader { get; set; } = true;
    protected override bool ChildWindow { get; set; } = false;
    protected bool FullCSVRows { get; set; } = true;
    protected bool TriggerSort { get; set; }
    protected static float Offset => -5f;

    public StatsList(Plugin plugin, SemaphoreSlim? interlock = null) : base(plugin, interlock) {
    }

    public override void DrawListItem(T item) {
        throw new NotImplementedException();
    }

    public override void OpenFullEditDetail(T item) {
        throw new NotImplementedException();
    }

    public override void OpenItemDetail(T item) {
    }

    protected override void PostRefresh(List<V> matches, List<V> additions, List<V> removals) {
        GoToPage(0);
        TriggerSort = true;
    }

    protected (PropertyInfo?, MemberInfo?) GetStatsPropertyFromId(uint columnId) {
        var props = typeof(U).GetProperties();
        //iterate up to two levels
        foreach(var prop in props) {
            var propId1 = $"{prop.Name}".GetHashCode();
            if((uint)propId1 == columnId) {
                return (prop, null);
            }

            var props2 = prop.PropertyType.GetProperties();
            var fields2 = prop.PropertyType.GetFields();
            List<MemberInfo> members2 = [.. props2, .. fields2];
            foreach(var member2 in members2) {
                var memberId2 = $"{prop.Name}.{member2.Name}".GetHashCode();
                if((uint)memberId2 == columnId) {
                    return (prop, member2);
                }
            }
        }
        return (null, null);
    }

    protected void SortByColumn(uint columnId, ImGuiSortDirection direction) {
        //_plugin.Log.Debug($"Sorting by {columnId}");
        Func<T, object>? comparator = null;
        if(typeof(T) == typeof(PlayerAlias)) {
            //0 = name
            //1 = homeworld
            if(columnId == 0) {
                comparator = (r) => (r as PlayerAlias)!.Name;
            } else if(columnId == 1) {
                comparator = (r) => (r as PlayerAlias)!.HomeWorld;
            }
        } else if(typeof(T) == typeof(Job)) {
            //0 = job
            //1 = role
            if(columnId == 0) {
                comparator = (r) => r;
            } else if(columnId == 1) {
                comparator = (r) => PlayerJobHelper.GetSubRoleFromJob((Job)Convert.ChangeType(r, typeof(Job))) ?? 0;
            }
        }

        if(comparator is null) {
            (var p1, var p2) = GetStatsPropertyFromId(columnId);
            if(p1 != null && p2 == null) {
                comparator = (r) => p1.GetValue(StatsModel[r]) ?? 0;
            } else if(p1 != null && p2 != null) {
                if(p2 is PropertyInfo) {
                    comparator = (r) => (p2 as PropertyInfo)!.GetValue(p1.GetValue(StatsModel[r])) ?? 0;
                } else if(p2 is FieldInfo) {
                    comparator = (r) => (p2 as FieldInfo)!.GetValue(p1.GetValue(StatsModel[r])) ?? 0;
                }
            } else {
                comparator = (r) => 0;
            }
        }

        DataModel = direction == ImGuiSortDirection.Ascending ? DataModel.OrderBy(comparator!).ToList() : DataModel.OrderByDescending(comparator!).ToList();
        DataModelUntruncated = direction == ImGuiSortDirection.Ascending ? DataModelUntruncated.OrderBy(comparator!).ToList() : DataModelUntruncated.OrderByDescending(comparator!).ToList();
    }

    protected string CSVRow(T key) {
        string csv = "";
        foreach(var col in Columns) {
            if(typeof(T) == typeof(PlayerAlias)) {
                //0 = name
                //1 = homeworld
                if(col.Id == 0) {
                    csv += (key as PlayerAlias)?.Name;
                } else if(col.Id == 1) {
                    csv += (key as PlayerAlias)?.HomeWorld;
                }
            } else if(typeof(T) == typeof(Job)) {
                //0 = job
                //1 = role
                if(col.Id == 0) {
                    csv += PlayerJobHelper.GetNameFromJob((Job)Convert.ChangeType(key, typeof(Job)));
                } else if(col.Id == 1) {
                    csv += PlayerJobHelper.GetSubRoleFromJob((Job)Convert.ChangeType(key, typeof(Job)));
                }
            }

            //find property
            (var p1, var p2) = GetStatsPropertyFromId(col.Id);
            if(p1 != null && p2 == null) {
                csv += p1.GetValue(StatsModel[key]);
            } else if(p1 != null && p2 != null) {
                if(p2 is PropertyInfo) {
                    csv += (p2 as PropertyInfo)!.GetValue(p1.GetValue(StatsModel[key]));
                } else if(p2 is FieldInfo) {
                    csv += (p2 as FieldInfo)!.GetValue(p1.GetValue(StatsModel[key]));
                }
            }
            csv += ",";
        }
        csv += "\n";
        return csv;
    }

    protected void CSVButton() {
        using(ImRaii.PushFont(UiBuilder.IconFont)) {
            if(ImGui.Button($"{FontAwesomeIcon.Copy.ToIconString()}##--CopyCSV")) {
                Task.Run(() => {
                    ListCSV = CSVHeader();
                    if(FullCSVRows) {
                        DataModel.ForEach((x) => ListCSV += CSVRow(x));
                    } else {
                        CurrentPage.ForEach((x) => ListCSV += CSVRow(x));
                    }
                    ImGui.SetClipboardText(ListCSV);
                });
            }
        }
        ImGuiHelper.WrappedTooltip("Copy CSV to clipboard");
    }

    protected override void PostColumnSetup() {
        ImGui.TableSetupScrollFreeze(1, 1);
        //column sorting
        ImGuiTableSortSpecsPtr sortSpecs = ImGui.TableGetSortSpecs();
        if(sortSpecs.SpecsDirty || TriggerSort) {
            //removed from data queue for performance reasons
            SortByColumn(sortSpecs.Specs.ColumnUserID, sortSpecs.Specs.SortDirection);
            GoToPage(0);
            TriggerSort = false;
            sortSpecs.SpecsDirty = false;
        }
    }
}
