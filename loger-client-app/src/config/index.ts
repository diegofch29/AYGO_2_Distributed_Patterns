interface Config {
  apiBaseUrl: string;
  apiPort: string;
}

const config: Config = {
  apiBaseUrl:
    import.meta.env.VITE_API_BASE_URL ||
    "http://ec2-54-147-56-151.compute-1.amazonaws.com",
  apiPort: import.meta.env.VITE_API_PORT || "8080",
};

// Normalize base URL and provide helper to build API endpoints
const normalizeBaseUrl = (base: string, port: string): string => {
  // Remove trailing slash
  let candidate = base.replace(/\/+$/g, "");

  try {
    // Ensure we have a protocol; URL() will throw if missing
    let url = new URL(candidate);

    // If URL already has a port, return normalized string
    if (url.port) return url.origin;

    // If no port, append configured port (only if not standard ports)
    if (port && !["80", "443"].includes(port)) {
      url.port = port;
      return url.origin;
    }

    return url.origin;
  } catch (e) {
    // If missing protocol, try to prepend http:// and try again
    try {
      let url = new URL(`http://${candidate}`);
      if (url.port) return url.origin;
      if (port && !["80", "443"].includes(port)) {
        url.port = port;
        return url.origin;
      }
      return url.origin;
    } catch (er) {
      // Fallback to input as-is
      return candidate;
    }
  }
};

const normalizedBase = normalizeBaseUrl(config.apiBaseUrl, config.apiPort);

export const getApiUrl = (): string => normalizedBase;

export const getApiEndpoint = (path: string): string => {
  // Ensure path starts with a slash
  const safePath = path.startsWith("/") ? path : `/${path}`;
  // Use URL to join base + path safely
  try {
    return new URL(safePath, getApiUrl()).toString();
  } catch (e) {
    // Fallback simple join
    return `${getApiUrl()}${safePath}`;
  }
};

// Debug output - remove or disable in production
/* eslint-disable no-console */
console.log("API Config:", {
  baseUrl: config.apiBaseUrl,
  port: config.apiPort,
  normalized: getApiUrl(),
});

export default config;
