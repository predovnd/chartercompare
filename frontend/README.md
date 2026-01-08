# Charter Compare Frontend

React + TypeScript + Vite frontend for Charter Compare.

## Setup

### Prerequisites
- Node.js 18+ and npm

### Installation

```bash
npm install
```

### Configuration

Create a `.env` file in this directory:

```env
VITE_API_URL=http://localhost:5000
```

### Development

```bash
npm run dev
```

The app will be available at `http://localhost:5173`

### Build

```bash
npm run build
```

### Preview Production Build

```bash
npm run preview
```

## Project Structure

```
frontend/
├── src/
│   ├── components/         # React components
│   │   ├── chat/           # Chat-related components
│   │   └── ui/             # shadcn/ui components
│   ├── lib/                # Utilities and API client
│   ├── types/              # TypeScript type definitions
│   └── ...
├── public/                 # Static assets
└── ...
```

## Tech Stack

- **React 18** - UI library
- **TypeScript** - Type safety
- **Vite** - Build tool and dev server
- **TailwindCSS** - Styling
- **shadcn/ui** - UI component library
- **lucide-react** - Icons
- **framer-motion** - Animations
