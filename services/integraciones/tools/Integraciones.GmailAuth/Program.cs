// Obtiene el refresh token de Gmail para el servicio Integraciones.
// Uso (desde services/integraciones):
//   dotnet run --project tools/Integraciones.GmailAuth -- ruta/al/client_secret.json
// Abre el navegador, inicias sesión con tu cuenta de Gmail, aceptas el permiso de solo lectura y la consola
// muestra los comandos de user-secrets que tienes que ejecutar. El token no se guarda en ningún fichero.

using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Gmail.v1;
using Google.Apis.Util.Store;

if (args.Length != 1 || !File.Exists(args[0]))
{
    Console.Error.WriteLine("Indica la ruta del JSON del cliente OAuth (tipo \"App de escritorio\") descargado de Google Cloud.");
    Console.Error.WriteLine("Ejemplo: dotnet run --project tools/Integraciones.GmailAuth -- C:\\Users\\yo\\Downloads\\client_secret.json");
    return 1;
}

var secrets = GoogleClientSecrets.FromFile(args[0]).Secrets;

var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
{
    ClientSecrets = secrets,
    Scopes = [GmailService.Scope.GmailReadonly], // solo lectura: no puede enviar, borrar ni cambiar nada
    DataStore = new NullDataStore(),             // no deja el token en disco
    Prompt = "consent"                           // fuerza a Google a devolver un refresh token aunque ya dieras permiso antes
});

Console.WriteLine("Se va a abrir el navegador. Inicia sesión con la cuenta de Gmail que recibe las alertas.");
var credential = await new AuthorizationCodeInstalledApp(flow, new LocalServerCodeReceiver()).AuthorizeAsync("user", CancellationToken.None);

if (string.IsNullOrEmpty(credential.Token.RefreshToken))
{
    Console.Error.WriteLine("Google no devolvió refresh token. Quita el acceso de la app en https://myaccount.google.com/permissions y repite.");
    return 1;
}

Console.WriteLine();
Console.WriteLine("Listo. Ejecuta esto desde services/integraciones para guardarlo en user-secrets:");
Console.WriteLine();
const string project = "--project src/Integraciones.Api";
Console.WriteLine($"dotnet user-secrets set \"Gmail:ClientId\" \"{secrets.ClientId}\" {project}");
Console.WriteLine($"dotnet user-secrets set \"Gmail:ClientSecret\" \"{secrets.ClientSecret}\" {project}");
Console.WriteLine($"dotnet user-secrets set \"Gmail:RefreshToken\" \"{credential.Token.RefreshToken}\" {project}");
return 0;
