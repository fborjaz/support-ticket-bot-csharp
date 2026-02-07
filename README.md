# 🤖 Bot de Soporte - Motor Conversacional en C#

Motor conversacional desarrollado en **ASP.NET Core (.NET 10)** que permite crear y consultar tickets de soporte mediante una interfaz de chat interactiva.

![.NET](https://img.shields.io/badge/.NET-10.0-purple)
![License](https://img.shields.io/badge/license-MIT-green)

## 📋 Características

- ✅ **Crear tickets de soporte** con flujo conversacional guiado
- ✅ **Consultar estado de tickets** por ID
- ✅ **Validación robusta** de entradas (nombre, email, descripción)
- ✅ **Manejo de sesiones** con timeout de 30 minutos
- ✅ **Límite de intentos fallidos** (máximo 5 por paso)
- ✅ **Integración OAuth 2.0** con cache de tokens
- ✅ **Interfaz web de chat** incluida
- ✅ **Detección inteligente de intenciones** (tolerante a mayúsculas/minúsculas)

## 🏗️ Arquitectura

El proyecto consta de dos servicios:

```
┌─────────────────────┐         ┌─────────────────────┐
│     BotEngine       │  HTTP   │    MockServices     │
│   (Puerto 5020)     │ ──────> │   (Puerto 5121)     │
│                     │  OAuth  │                     │
│  - Chat UI          │         │  - /oauth/token     │
│  - /messages        │         │  - /tickets         │
└─────────────────────┘         └─────────────────────┘
```

### BotEngine (Puerto 5020)

Motor conversacional principal con:

- Endpoint `POST /messages` para procesar mensajes
- Interfaz web de chat en `/`
- Manejo de estado por conversación
- Cliente OAuth para autenticación

### MockServices (Puerto 5121)

API mock que simula el servicio externo de tickets:

- `POST /oauth/token` - Autenticación OAuth 2.0
- `POST /tickets` - Crear ticket
- `GET /tickets/{id}` - Consultar ticket

## 🚀 Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Terminal/CMD
- Navegador web

## ⚙️ Instalación

1. **Clonar el repositorio**

```bash
git clone https://github.com/fborjaz/support-ticket-bot-csharp.git
cd support-ticket-bot-csharp
```

2. **Restaurar dependencias**

```bash
dotnet restore
```

3. **Compilar el proyecto**

```bash
dotnet build
```

## ▶️ Ejecución

### Opción 1: Ejecutar ambos servicios (recomendado)

Necesitas **dos terminales**:

**Terminal 1 - MockServices:**

```bash
cd MockServices
dotnet run
```

**Terminal 2 - BotEngine:**

```bash
cd BotEngine
dotnet run
```

El navegador se abrirá automáticamente en `http://localhost:5020` con la interfaz de chat.

### Opción 2: Usando el archivo .http (VS Code)

Si tienes la extensión REST Client en VS Code, puedes usar los archivos `.http` incluidos para probar los endpoints directamente.

## 💬 Uso del Bot

### Interfaz Web

1. Accede a `http://localhost:5020`
2. Usa los botones rápidos o escribe:
   - `"Quiero crear un ticket"` - Inicia el flujo de creación
   - `"Ver estado del ticket TCK-001"` - Consulta un ticket
   - `"cancelar"` - Cancela el proceso actual

### API REST

**Crear/Consultar mediante mensajes:**

```bash
curl -X POST http://localhost:5020/messages \
  -H "Content-Type: application/json" \
  -d '{
    "conversationId": "user-123",
    "message": "Quiero crear un ticket"
  }'
```

**Respuesta:**

```json
{
  "conversationId": "user-123",
  "reply": "🎫 **CREAR NUEVO TICKET**\n══════════════════════════\n\n📊 Progreso: [▓░░] 1/3\n\n👤 ¿Cuál es tu **nombre**?",
  "hasActiveFlow": true,
  "activeFlow": "CreateTicket"
}
```

## 📁 Estructura del Proyecto

```
├── BotEngine/
│   ├── Controllers/
│   │   └── MessagesController.cs    # Endpoint principal
│   ├── DTOs/                        # Data Transfer Objects
│   ├── Models/
│   │   ├── ConversationContext.cs   # Estado de conversación
│   │   ├── ConversationFlow.cs      # Enums de flujos
│   │   ├── TicketData.cs            # Datos del ticket
│   │   └── ValidationConstants.cs   # Constantes de validación
│   ├── Services/
│   │   ├── ConversationFlowService.cs    # Lógica del bot
│   │   ├── ConversationStateService.cs   # Manejo de estado
│   │   ├── ExternalTicketService.cs      # Cliente HTTP + OAuth
│   │   └── InputValidationService.cs     # Validaciones
│   ├── wwwroot/
│   │   └── index.html               # Interfaz de chat
│   └── Program.cs                   # Configuración de la app
│
├── MockServices/
│   ├── Controllers/
│   │   ├── OAuthController.cs       # Autenticación
│   │   └── TicketsController.cs     # CRUD de tickets
│   └── Models/                      # Modelos de datos
│
└── BotSupport.slnx                  # Solución
```

## 🔧 Configuración

### BotEngine/appsettings.json

```json
{
  "ExternalServices": {
    "BaseUrl": "http://localhost:5121",
    "ClientId": "bot-client",
    "ClientSecret": "bot-secret"
  }
}
```

## ✅ Flujo de Creación de Ticket

```
Usuario: "Quiero crear un ticket"
Bot: Solicita nombre → Valida →
Bot: Solicita email → Valida formato →
Bot: Solicita descripción → Valida →
Bot: Muestra resumen → Pide confirmación →
Bot: Crea ticket → Retorna ID (TCK-XXX)
```

**Validaciones incluidas:**

- Nombre: 2-100 caracteres, solo letras
- Email: Formato válido, detecta typos comunes (gmial→gmail)
- Descripción: 10-1000 caracteres
- Detecta comandos en campos (evita "crear ticket" como nombre)

## 🛡️ Seguridad

- Sanitización de entradas contra XSS
- Validación de patrones peligrosos
- Tokens OAuth con expiración y renovación automática
- Límite de intentos por paso

## 📝 Licencia

MIT License - Ver [LICENSE](LICENSE) para más detalles.

## 👨‍💻 Autor

Desarrollado como prueba técnica de motor conversacional en C#.
