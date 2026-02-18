# Jämförelse och analys – GET /health

## Kodjämförelse

| Aspekt                    | Novis                 | Junior                          | Senior                                          |
| ------------------------- | --------------------- | ------------------------------- | ----------------------------------------------- |
| **Filer**                 | 1 (enbart controller) | 2 (controller + service)        | 2 (controller + service m. interface)           |
| **Interface för service** | Nej                   | Nej                             | Ja (`IHealthService`)                           |
| **Returtyp från service** | –                     | `IActionResult`                 | `HealthResponse` (domänobjekt)                  |
| **Svarsdataformat**       | Sträng: `"ok"`        | Objekt: `{ status: "Healthy" }` | Objekt: `{ status: "Healthy", timestamp: ... }` |
| **Dependency Injection**  | Nej                   | Ja (konkret klass)              | Ja (abstraktion/interface)                      |
| **Testbarhet**            | Svår                  | Medel                           | God                                             |

---

## Novis – Analys

```csharp
public IActionResult Get()
{
    return Ok("ok");
}
```

All logik, om man ens kan kalla det logik, finns direkt i controllern. Den uppfyller kravet "returnera ok" men inget mer. Inga abstraktioner, inga lager, ingen separation av ansvar.

**Styrkor:** Minimal och omedelbart begriplig. Funkar.

**Svagheter:**

- Bryter mot SRP – controllern har hand om allt.
- Ingen testbarhet; man kan inte mocka bort logiken.
- Svarssträngen `"ok"` är inte ett strukturerat API-svar, vilket gör det svårt för konsumenter att parsa.
- Skalbarhet saknas – om man vill lägga till t.ex. databaskontroll måste man ändra inuti controllern (bryter mot OCP).

---

## Junior – Analys

```csharp
// Controller
public HealthController(HealthService healthService) { ... }
public IActionResult Get() => _healthService.CheckHealth();

// Service
public IActionResult CheckHealth() => new OkObjectResult(new { status = "Healthy" });
```

Controllern delegerar till en service, vilket är rätt riktning. Separation of Concerns är påbörjad.

**Styrkor:** Controllern är tunn. Logiken är utflyttad. Strukturerat JSON-svar.

**Svagheter:**

- Beror på den **konkreta klassen** `HealthService`, inte ett interface – bryter mot DIP. Det gör att man inte kan mocka servicen i tester.
- Servicen returnerar `IActionResult`, vilket är ett HTTP/MVC-koncept. En service ska inte känna till HTTP-lagret – detta är ett tydligt brott mot SoC och lagerarkitekturen.
- Eftersom `HealthService` är injicerad som konkret klass, om man byter implementation måste man ändra i controllern – bryter mot OCP.

---

## Senior – Analys

```csharp
// Interface
public interface IHealthService { HealthResponse GetHealthStatus(); }

// Service
public class HealthService : IHealthService
{
    public HealthResponse GetHealthStatus() =>
        new HealthResponse { Status = "Healthy", Timestamp = DateTime.UtcNow };
}

// Controller
public HealthController(IHealthService healthService) { ... }
public IActionResult Get() { var s = _healthService.GetHealthStatus(); return Ok(s); }
```

**Styrkor:**

- Controllern beror på `IHealthService` (abstraktion) – uppfyller DIP fullt ut.
- Servicen returnerar ett **domänobjekt** (`HealthResponse`), inte ett HTTP-objekt. Servicen är helt omedveten om HTTP-lagret – SoC och Layered Architecture följs korrekt.
- `IHealthService` kan mockas i enhetstester utan att röra verklig logik.
- Ny funktionalitet (t.ex. databaskontroll, minneskontroll) kan läggas till i `GetHealthStatus()` utan att ändra controllern – OCP uppfylls.
- `Timestamp` ger observability "gratis" – man kan direkt se när senaste hälsokontroll gjordes.
- `HealthResponse` följer DRY; svarsstrukturen är definierad på ett ställe.

**Svagheter (marginella):**

- Något mer kod för ett enkelt syfte, men det är ett medvetet arkitektoniskt val, inte over-engineering i detta sammanhang.

---

## Sammanfattande slutsats

De tre nivåerna illustrerar tydligt Dreyfus-modellens progression:

- **Novis** klarar kravet men utan medvetenhet om varför struktur behövs.
- **Junior** förstår att man ska separera lager men appliceringen är inkonsekvent – den viktigaste principen (DIP) missas, och servicens returtyp läcker HTTP-koncept ned i fel lager.
- **Senior** tillämpar principerna konsekvent och av rätt anledning: varje klass kan ändras, bytas ut eller testas isolerat utan att påverka de andra.

Det mest konkreta och tekniskt signifikanta skillnaden är att Junior beror på en **konkret klass** och returnerar `IActionResult` från servicen, medan Senior beror på ett **interface** och returnerar ett rent domänobjekt. Dessa två skillnader har direkt påverkan på testbarhet, underhållbarhet och möjligheten att följa OCP vid framtida utökning.
