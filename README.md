# WeatherApp 🌤️

A modern, containerized Blazor weather dashboard featuring real-time data, HTML5 geolocation, and a responsive Tailwind CSS UI with Dark Mode.

## 🚀 Features
* **Real-time Weather:** Fetches live data from OpenWeatherMap.
* **Geolocation:** Automatically detects your location using the browser's HTML5 API.
* **Modern UI:** Built with Tailwind CSS v4 for a clean, responsive design.
* **Dark Mode:** Supports system-level and manual dark mode toggling.
* **Containerized:** Ready for deployment anywhere using Docker.

## 🛠️ Tech Stack
* **Framework:** .NET 10 Blazor
* **Styling:** Tailwind CSS v4
* **Data Source:** OpenWeatherMap API
* **Deployment:** Docker (Multi-stage build)

## 📦 Quick Start (Using Docker)

The easiest way to run the app without installing the .NET SDK or Node.js locally is via Docker.

1. **Build the image:**
   ```bash
   docker build -t weatherapp .