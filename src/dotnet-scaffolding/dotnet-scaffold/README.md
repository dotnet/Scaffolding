New and improved scaffolding experience. 
More details coming soon!

## Blazor Identity

Blazor Identity scaffolding analyzes the app's interactivity and referenced WebAssembly client for all supported target frameworks. For .NET 9 and later, it configures authentication for static SSR, Blazor Server, WebAssembly, and Auto, including global interactivity. Account pages remain statically rendered, and WebAssembly/Auto apps receive authentication-state serialization on the server and deserialization on the client.

.NET 8 retains its existing generation behavior because its authentication-state persistence and routing mechanisms differ. Generated account pages and antiforgery handling remain specific to the target framework.