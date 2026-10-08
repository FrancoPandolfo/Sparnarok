using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Sparnarok.Tests.E2E;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class QuestBoardTests : PageTest
{
    [Test]
    public async Task DragQuestToCompleted_ShouldShowXpPopupAndUpdateRank()
    {
        // Navegamos al frontend. Asumimos el puerto 5174 porque el 5173 quedó colgado en tu sesión.
        await Page.GotoAsync("http://localhost:5174/login");
        await Page.EvaluateAsync("window.localStorage.setItem('sparnarok-auth', '{\"state\":{\"token\":\"mock-token\",\"username\":\"Hero\"},\"version\":0}')");
        await Page.GotoAsync("http://localhost:5174/");

        // El usuario debe empezar como Novato (0 XP base simulada)
        await Expect(Page.Locator("text=Rank: Novato")).ToBeVisibleAsync();
        
        // Identificamos la Quest
        var card = Page.Locator("text=Refactorizar Auth a Middleware");
        await Expect(card).ToBeVisibleAsync();

        // Identificamos la zona donde se sueltan las tarjetas de la columna "Completada" (la 3ra columna)
        var destColumn = Page.Locator(".min-h-\\[200px\\]").Nth(2);

        // Simulamos el Drag and Drop
        await card.DragToAsync(destColumn);

        // Verificamos que el Popup de XP aparece en pantalla
        var popup = Page.Locator("text=XP Ganada");
        await Expect(popup).ToBeVisibleAsync(new() { Timeout = 3000 });

        // Verificamos que el Header se actualizó dinámicamente a Aventurero
        await Expect(Page.Locator("text=Rank: Aventurero")).ToBeVisibleAsync();
    }
}
