# Charter Compare

A modern web application for comparing charter bus quotes, built with React + TypeScript + Vite frontend and .NET Core backend.

## Project Structure

```
chartercompare/
├── frontend/               # Frontend (React + TypeScript + Vite)
│   ├── src/                # Source code
│   ├── package.json        # Dependencies
│   ├── vite.config.ts      # Vite configuration
│   └── ...
├── backend/                # Backend (.NET 8.0 Web API)
│   ├── CharterCompare.Api/ # API project
│   ├── CharterCompare.sln  # Solution file
│   └── README.md           # Backend documentation
└── README.md               # This file
```

## Frontend Setup

### Prerequisites
- Node.js 18+ and npm

### Installation

```bash
cd frontend
npm install
```

### Configuration

Create a `.env` file in the `frontend/` directory:

```env
VITE_API_URL=http://localhost:5000
```

### Run Development Server

```bash
cd frontend
npm run dev
```

The frontend will be available at `http://localhost:5173`

## Backend Setup

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Run Backend

```bash
cd backend/CharterCompare.Api
dotnet restore
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `https://localhost:5001/swagger`

## Development Workflow

1. **Start the backend** first:
   ```bash
   cd backend/CharterCompare.Api
   dotnet run
   ```

2. **Start the frontend** in a separate terminal:
   ```bash
   cd frontend
   npm run dev
   ```

3. Open `http://localhost:5173` in your browser

## API Endpoints

- `POST /api/chat/start` - Start a new chat session
- `POST /api/chat/message` - Send a message and get response

See `backend/README.md` for detailed API documentation.

## Features

- ✅ Modern React + TypeScript frontend
- ✅ .NET 8.0 Web API backend
- ✅ Real-time chat interface
- ✅ Context-sensitive icons
- ✅ Responsive design
- ✅ CORS configured for local development
- ✅ Swagger/OpenAPI documentation

## Next Steps

- [ ] Add database persistence
- [ ] Deploy to Azure
- [ ] Add authentication
- [ ] Implement email/SMS notifications
- [ ] Add analytics and monitoring
