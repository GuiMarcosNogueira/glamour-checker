using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using GlamourChecker.Core;
using System.Numerics;

namespace GlamourChecker.Windows;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class UpgradeAdvisorWindow : Window
{
    private readonly UpgradeAdvisorService _upgradeAdvisorService;

    public UpgradeAdvisorWindow(UpgradeAdvisorService upgradeAdvisorService)
        : base("Dresser Upgrades###GlamourCheckerUpgradeAdvisor")
    {
        _upgradeAdvisorService = upgradeAdvisorService;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(400, 300),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public override void Draw()
    {
        var upgrades = _upgradeAdvisorService.CurrentUpgrades;

        if (upgrades.Count == 0)
        {
            ImGui.Text(string.Format(Loc.Localize("Message_NoUpgradesFound", "Nenhum upgrade encontrado no seu Glamour Dresser / Armoire para o {0}."), _upgradeAdvisorService.CurrentJobAbbrev));
            return;
        }

        ImGui.Text(string.Format(Loc.Localize("Message_UpgradesFoundWindow", "Encontrados {0} itens no Dresser/Armoire que melhoram o seu {1}:"), upgrades.Count, _upgradeAdvisorService.CurrentJobAbbrev));
        ImGui.Spacing();

        if (ImGui.BeginTable("upgrades_table", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable))
        {
            ImGui.TableSetupColumn(Loc.Localize("Column_Item", "Item"));
            ImGui.TableSetupColumn(Loc.Localize("Column_Slot", "Slot"));
            ImGui.TableSetupColumn(Loc.Localize("Column_iLvlSeu", "iLvl (Seu)"));
            ImGui.TableSetupColumn(Loc.Localize("Column_iLvlNovo", "iLvl (Novo)"));
            ImGui.TableHeadersRow();

            foreach (var upgrade in upgrades)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                string source = upgrade.IsFromArmoire ? "[Armoire] " : "[Dresser] ";
                ImGui.Text(source + upgrade.Name);

                ImGui.TableNextColumn();
                ImGui.Text(upgrade.SlotName);

                ImGui.TableNextColumn();
                ImGui.Text(upgrade.EquippedItemLevel.ToString());

                ImGui.TableNextColumn();
                // Highlight the new ilvl in green
                ImGui.TextColored(new Vector4(0, 1, 0, 1), upgrade.ItemLevel.ToString());
            }

            ImGui.EndTable();
        }
    }
}

