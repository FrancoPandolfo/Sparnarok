using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Sparnarok.Tests.E2E;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class CreateQuestTests : PageTest
{
    [Test]
    public async Task User_Can_Create_A_Quest_Successfully()
    {
        // Arrange
        // Asumiendo que el frontend corre localmente en el puerto 5173
        await Page.GotoAsync("http://localhost:5173/quests/new");

        // Act
        await Page.FillAsync("id=questName", "Derrotar al Bug del Login");
        await Page.FillAsync("id=questDesc", "El login devuelve 500 a veces.");
        await Page.ClickAsync("button[type='submit']");

        // Assert
        // Verificamos que aparece el mensaje de éxito
        var successMessage = Page.Locator("text=¡Quest forjada con éxito!");
        await Expect(successMessage).ToBeVisibleAsync();
    }
}
