using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Sparnarok.Tests.E2E;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class FullIntegrationE2ETests : PageTest
{
    [Test]
    public async Task Manager_HappyPath_CalibrationAndQuestCompletion()
    {
        // Navegar al dashboard
        await Page.GotoAsync("http://localhost:5174/party/dashboard");
        
        // Esperar que cargue el dashboard
        await Expect(Page.GetByText("Manager Analytics")).ToBeVisibleAsync();

        // Verificar y clickear calibrar en "Sam" (Mock)
        var calibrateButton = Page.Locator("button:has-text('Calibrar')").First;
        await Expect(calibrateButton).ToBeVisibleAsync();
        await calibrateButton.ClickAsync();

        // Esperar que el modal de Sesión Cero aparezca
        await Expect(Page.GetByText("Sesión Cero")).ToBeVisibleAsync();
        
        // Calibrar a Senior
        var select = Page.GetByTestId("seniority-select");
        await select.SelectOptionAsync("Senior");

        // Guardar calibración
        var saveBtn = Page.GetByText("Guardar Calibración");
        await saveBtn.ClickAsync();

        // Verificar optimismo (botón de calibrar ya no está visible para ese usuario)
        // y su XP se actualizó (Mock asume que se actualizó)
        await Expect(calibrateButton).ToBeHiddenAsync();

        // Navegar a tablero de quests
        await Page.GotoAsync("http://localhost:5174/");

        // Crear una nueva quest
        var newQuestInput = Page.Locator("input[placeholder='Nueva Quest...']");
        await Expect(newQuestInput).ToBeVisibleAsync(new() { Timeout = 10000 });
        await newQuestInput.FillAsync("E2E Integration Test Quest");
        await newQuestInput.PressAsync("Enter");

        // Drag & Drop
        var questCard = Page.Locator("text=E2E Integration Test Quest");
        await Expect(questCard).ToBeVisibleAsync();

        var completedColumn = Page.Locator("div:has-text('COMPLETADAS')").Last;
        
        // Simulate Drag and Drop
        await questCard.DragToAsync(completedColumn);

        // Verify XP animation pops up
        await Expect(Page.Locator("text=+50 XP")).ToBeVisibleAsync();
    }
}
