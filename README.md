# arquitectura-limpia

Ejemplo práctico de arquitectura limpia con .NET 6

## SMTP
Para envío de emails de prueba se puede utilizar un servidor SMTP de prueba con Docker
```
docker run --rm -it -p 3001:80 -p 25:25 rnwood/smtp4dev
```

y conectarse a `smtp` en el puerto `25`.

La UI está accesible en http://localhost:3001 para ver los emails enviados.

## Modelado del problema
Aplicación completa con Api REST en la que se crean partidos de fútbol, se inician, se terminan y se anotan goles. 

Además se puede consultar el marcador en cualquier momento, y cada vez que se inicia o termina un partido, se envía una notificación por email a una dirección predefinida `sample-list@test.com`

## Implementación de la capa de Dominio
Se importa solamente el proyecto de `Soccer.Domain`, que ya tiene clases de excepciones personalizadas para escenarios de error que se pueden revisar.

El principal objeto de dominio, protector de invariantes y el que marca las fronteras transaccionales, es `Game`.

Esta clase debe contener todos los comandos disponibles
- Crear (implementado en el constructor)
- Start
- End
- ScoreGoal

Vamos a necesitar enviar notificaciones, pero al dominio no le importa cómo se envían exactamente o con qué tecnología externa.

Se importa el proyecto que define esa abstracción `Soccer.Notification.Abstractions` y que contiene la interfaz
```
public interface INotifier
{
    void Notify(string subject, string message, params string [] destination);
}
```

Se añade una dependencia de `Soccer.Domain` a `Soccer.Notification.Abstractions`.

Se crea una clase, o record, `Goal` que modela goles. Se elige un tipo de objeto de dominio value object, que se puede representar con un record
```
public record Goal(DateTime ScoredOn, string ScoredBy);
```

Y ahora se comienza a crear toda la lógica de negocio en `Game`

```
public class Game
{
    public Guid Id { get; }
    public string LocalTeamCode { get; }
    public string AwayTeamCode { get; }
    public DateTime? StartedOn { get; private set; }
    public DateTime? EndedOn { get; private set; }
    public bool IsInProgress => StartedOn.HasValue && !EndedOn.HasValue;
    public bool IsEnded => EndedOn.HasValue && StartedOn.HasValue;

    private readonly List<Goal> _localTeamGoals = new();
    public IReadOnlyCollection<Goal> LocalTeamGoals => _localTeamGoals.AsReadOnly();
    private readonly List<Goal> _awayTeamGoals = new();
    public IReadOnlyCollection<Goal> AwayTeamGoals => _awayTeamGoals.AsReadOnly();


    public Game(Guid id, string localTeamCode, string awayTeamCode)
    {
        if (!IsValidTeam(localTeamCode))
        {
            throw new InvalidTeamException(localTeamCode);
        }

        if (!IsValidTeam(awayTeamCode))
        {
            throw new InvalidTeamException(awayTeamCode);
        }

        Id = id;
        LocalTeamCode = localTeamCode;
        AwayTeamCode = awayTeamCode;
    }

    public void Start(DateTime startedOn, INotifier notifier)
    {
        if (IsInProgress)
        {
            throw new GameInProgressException(Id);
        }

        if (IsEnded)
        {
            throw new GameEndedException(Id);
        }

        StartedOn = startedOn;

        notifier
            .Notify(
                $"Game {Id} started",
                $"The game between {LocalTeamCode} and {AwayTeamCode} has started on {StartedOn}",
                LocalTeamCode,
                AwayTeamCode);
    }

    public void End(DateTime endedOn, INotifier notifier)
    {
        if (!IsInProgress)
        {
            throw new GameNotInProgressException(Id);
        }

        if (endedOn <= StartedOn)
        {
            throw new InvalidGameActionException($"The game started on {StartedOn} and cannot end prior to that time");
        }

        EndedOn = endedOn;

        notifier
            .Notify(
                $"Game {Id} ended",
                $"The game between {LocalTeamCode} and {AwayTeamCode} has ended on {EndedOn}",
                LocalTeamCode,
                AwayTeamCode);
    }

    public void ScoreGoal(Goal goal, bool isLocalTeam)
    {
        if (!IsInProgress)
        {
            throw new GameNotInProgressException(Id);
        }

        if (isLocalTeam)
        {
            _localTeamGoals.Add(goal);
        }
        else
        {
            _awayTeamGoals.Add(goal);
        }
    }
    
    private bool IsValidTeam(string teamCode)
    {
        return teamCode.Length == 3 && !teamCode.Any(char.IsLower);
    }
}
```

Esta capa de dominio se podría probar en aislamiento, pero las pruebas automáticas no son objetivo de este curso. Como ejemplo se puede crear el siguiente test unitario, que demuestra que 
se puede mockear el comportamiento de notificación.

Importa el proyecto `Soccer.Domain.UnitTests`, que tiene dependencias con xUnit, FluentAssertions y Moq, y crea un `GameTests/StartTests`, por ejemplo.

```
[Fact]
public void Given_A_Non_Started_Game_When_Starting_It_Should_Become_In_Progress()
{
    // Given
    var id = Guid.Empty;
    var localTeamCode = "RMA";
    var awayTeamCode = "BAR";
    var sut = new Game(id, localTeamCode, awayTeamCode);

    var startedOn = new DateTime(2022, 3, 1, 18, 0, 0);
    var notifierMock = new Mock<INotifier>();
    var notifier = notifierMock.Object;

    // When
    sut.Start(startedOn, notifier);

    // Then
    sut.IsInProgress.Should().BeTrue();
    sut.IsEnded.Should().BeFalse();
    sut.StartedOn.Should().Be(startedOn);
    notifierMock.Verify(x => x.Notify($"Game {id} started", It.IsAny<string>(), localTeamCode, awayTeamCode), Times.Once);
}
```

## Implementación de la capa de Aplicación
Se importa el proyecto `Soccer.Persistence.Abstractions` que contiene el contrato para persistir y recuperar partidos
```
public interface IGameRepository
{
    void Upsert(Game game);
    Game GetGame(Guid id);
}
```

A continuación, se importa el proyecto `Soccer.Application` que debe depender de `Soccer.Persistence.Abstractions`.
Se explora sus modelos. Tiene modelos de escritura y modelos de lectura que se expondrán, más adelante, a través de la Api REST en la capa de infraestructura.

Para las operaciones de escritura, se debe completar el `GameCommandService` que dependerá del repositorio para recuperar y guardar partidos, y del servicio de notificaciones que le inyectará a los métodos de negocio de `Game`.
También depende de una factoría que sirve para crear la fecha actual en UTC y que se abstrae simplemente para poder modificar su implementación (o mockearla) a conveniencia.

Se irá creando el `CreateNewGame`, `SetProgress` y `Score`, que son los 3 casos de uso a utilizar.

```
public class GameCommandService
{
    private readonly IGameRepository _gameRepository;
    private readonly IDateTimeFactory _dateTimeFactory;
    private readonly INotifier _notifier;

    public GameCommandService(
        IGameRepository gameRepository,
        IDateTimeFactory dateTimeFactory,
        INotifier notifier)
    {
        _gameRepository = gameRepository;
        _dateTimeFactory = dateTimeFactory;
        _notifier = notifier;
    }

    public Guid CreateGame(NewGame newGame)
    {
        var newId = Guid.NewGuid();
        var game = new Game(newId, newGame.LocalTeamCode, newGame.ForeignTeamCode);

        _gameRepository.Upsert(game);

        return game.Id;
    }

    public void SetProgress(Guid gameId, GameProgress gameProgress)
    {
        var game = _gameRepository.GetGame(gameId);
        var currentDate = _dateTimeFactory.CreateUtcNow();
        if (gameProgress.IsInProgress)
        {
            game.Start(currentDate, _notifier);
        }
        else
        {
            game.End(currentDate, _notifier);
        }

        _gameRepository.Upsert(game);
    }

    public void Score(Guid id, NewGoal newGoal)
    {
        var game = _gameRepository.GetGame(id);
        var currentDate = _dateTimeFactory.CreateUtcNow();
        var teamCode = newGoal.TeamCode;
        var goal = new Goal(currentDate, newGoal.ScoredBy);
        var isTeamPlaying = game.LocalTeamCode == teamCode || game.AwayTeamCode == teamCode;
        if (!isTeamPlaying)
        {
            throw new KeyNotFoundException($"The team code {teamCode} is not playing the game");
        }

        var isLocalTeam = game.LocalTeamCode == teamCode;
        game.ScoreGoal(goal, isLocalTeam);

        _gameRepository.Upsert(game);
    }
}
```

Se continúa por las operaciones de lectura en `GameQueryService`, que depende del repositorio al que accede para recuperar partidos, y de un mapeador que utiliza para convertir objetos de dominio `Game` 
en su representación de datos `ScoreBoard`, que será lo que devuelva en respuestas http.

Este servicio implementa el caso de uso `GetScoreBoard`.

```
public class GameQueryService
{
    private readonly IGameRepository _gameRepository;
    private readonly GameToScoreBoardMapper _gameToScoreBoardMapper;

    public GameQueryService(
        IGameRepository gameRepository,
        GameToScoreBoardMapper gameToScoreBoardMapper)
    {
        _gameRepository = gameRepository;
        _gameToScoreBoardMapper = gameToScoreBoardMapper;
    }

    public ScoreBoard GetScoreBoard(Guid id)
    {
        var game = _gameRepository.GetGame(id);
        var gameReport = _gameToScoreBoardMapper.Map(game);
        return gameReport;
    }
}
```

## Implementación del repositorio en la capa de infraestructura
Se comienza a implementar la capa de infraestructura con una persistencia muy simple, en memoria.

Se importa el proyecto `Soccer.Persistence.InMemory` y se implementa su `GameRepositoryInMemory` con un simple diccionario. Este proyecto depende del proyecto de `Soccer.Persistence.Abstractions`
y para que funcione correctamente, esta clase debería ser singleton o registrarse como singleton más adelante en el contenedor de dependencias.

```
public class GameRepositoryInMemory
    : IGameRepository
{
    private readonly IDictionary<Guid, Game> _games = new Dictionary<Guid, Game>();

    public void Upsert(Game game)
    {
        _games[game.Id] = game;
    }

    public Game GetGame(Guid id)
    {
        return _games[id];
    }
}
```

## Implementación del notificador en la capa de infraestructura
Las notificaciones se pueden enviar por email a una dirección dada. En el futuro se pueden añadir más tipos de notificaciones.

Se importa el proyecto `Soccer.Notification.Email` que contiene una dependencia al paquete nuget `MailKit`. A Dominio y Aplicación le resulta irrelevante cómo se implemente esta capa de infraestructura.

Tiene un modelo que sirve para configuración del servidor SMTP y el servicio en sí `EmailNotifier`, que depende de esa configuración y que se le inyecta.
```
public class EmailNotifier
    : INotifier
{
    private readonly SmtpConfiguration _smtpConfiguration;

    public EmailNotifier(SmtpConfiguration smtpConfiguration)
    {
        _smtpConfiguration = smtpConfiguration;
    }

    public void Notify(string subject, string message, params string[] destination)
    {
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress("OW", _smtpConfiguration.From));
        mimeMessage.To.Add(new MailboxAddress("Clubs", _smtpConfiguration.To));
        mimeMessage.Subject = subject;
        mimeMessage.Body = new TextPart("plain")
        {
            Text = message
        };

        using var smtpClient = new SmtpClient();
        smtpClient.Connect(_smtpConfiguration.Hostname, _smtpConfiguration.Port, false);
        smtpClient.Send(mimeMessage);
        smtpClient.Disconnect(true);
    }
}
```

## Implementación de la Api REST en la capa de infraestructura
Por último, se importa el proyecto principal, el que va a contener los endpoint HTTP y el contenedor de dependencias. El proyecto que el servidor Kestrel ejecuta.

Se importa `Soccer` y se completa su controlador `GamesController` para que exponga todos los endpoint http.

Este controlador delega en `GameCommandService` y `GameQueryService` para que orqueste los casos de uso de escritura y lectura.

```
[ApiController]
[Route("[controller]")]
public class GamesController
    : ControllerBase
{
    private readonly GameCommandService _gameCommandService;
    private readonly GameQueryService _gameQueryService;

    public GamesController(
        GameCommandService gameCommandService,
        GameQueryService gameQueryService)
    {
        _gameCommandService = gameCommandService;
        _gameQueryService = gameQueryService;
    }
    
    /// <param name="id" example="00000000-0000-0000-0000-000000000000">The game id</param>
    [HttpGet("{id}")]
    public IActionResult GetScoreBoard(Guid id)
    {
        var scoreBoard = _gameQueryService.GetScoreBoard(id);
        return Ok(scoreBoard);
    }

    [HttpPost]
    public IActionResult CreateGame([FromBody] NewGame newGame)
    {
        var id = _gameCommandService.CreateGame(newGame);
        return CreatedAtAction(nameof(GetScoreBoard), new { id = id }, newGame);
    }

    /// <param name="id" example="00000000-0000-0000-0000-000000000000">The game id</param>
    /// <param name="gameProgress">The patch game object containing the isInProgress property</param>
    [HttpPatch("{id}")]
    public IActionResult StartGame(Guid id, [FromBody] GameProgress gameProgress)
    {
        _gameCommandService.SetProgress(id, gameProgress);
        return Ok();
    }

    /// <param name="id" example="00000000-0000-0000-0000-000000000000">The game id</param>
    /// <param name="newGoal">The new goal containing the team that scores and the scorer</param>
    [HttpPost("{id}/goals")]
    public IActionResult AddGoal(Guid id, [FromBody] NewGoal newGoal)
    {
        _gameCommandService.Score(id, newGoal);
        return Ok();
    }
}
```

## Configurando el contenedor de dependencias
Una vez listo el controlador, debemos implementar el `Program.cs` que es la puerta de entrada a la funcionalidad de AspNetCore y que contiene el contenedor de dependencias y la pipeline http (e.g: middlewares).

Se puede observar que todos los servicios se registran en el contenedor de dependencias y que la pipeline expone el controlador http para que responda a las peticiones.

```
using Soccer.Application;
using Soccer.Application.Factories;
using Soccer.Application.Mappers;
using Soccer.Notification.Abstractions;
using Soccer.Notification.Email;
using Soccer.Notification.Email.Models;
using Soccer.Persistence.Abstractions;
using Soccer.Persistence.InMemory;
using Soccer.WebApi;

var builder = WebApplication.CreateBuilder(args);

// IoCC
var services = builder.Services;
services.AddTransient<GameToScoreBoardMapper>();
services.AddSingleton<IDateTimeFactory, DateTimeFactory>();
services.AddTransient<GameCommandService>();
services.AddTransient<GameQueryService>();
services
    .AddTransient(sp =>
    {
        var smtpConfiguration =
            new SmtpConfiguration
            {
                Hostname = "smtp",
                Port = 25
            };
        return smtpConfiguration;
    });
services.AddSingleton<IGameRepository, GameRepositoryInMemory>();
services.AddTransient<INotifier, EmailNotifier>();
services.AddControllers();

var app = builder.Build();

// Http Pipeline
app.UseRouting();
app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

app.Run();
```

## Añadiendo una UI con swagger y Open API
Como no hay un front-end para interactuar con la funcionalidad, instalamos OpenApi middleware.

Basta con añadir una llamada a un método de extensión que contiene toda la configuración en el registro de dependencias del contenedor.
```
services.AddOpenApi(); // OpenAPI
```

y enchufar el middleware al comienzo de la pipeline http
```
app.UseOpenApi();
```

Toda la aplicación debería compilar sin problemas y estamos listos para probarla a través de la UI de swagger.

## Ejecutando la solución
Se puede ejecutar con Kestrel el proyecto principal desde consola o desde el IDE.
```
dotnet run --project src/Soccer.WebApi
```

No hay que olvidar que se requiere un servidor SMTP para que las notificaciones funcionen, por lo que también deberá existir en el puerto correcto. Se puede usar docker
```
docker run --rm -it -p 3001:80 -p 25:25 rnwood/smtp4dev
```

La aplicación está disponible en http://localhost:3000/swagger

y el servidor SMTP tiene una UI disponible en http://localhost:3001