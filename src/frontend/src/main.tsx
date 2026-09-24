import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import { AuthBootstrap } from "@/features/auth/components/AuthBootstrap";
import App from "./App";
import "./index.css";
import { LanguageProvider } from "@/i18n/LanguageProvider";
import { useAuthStore } from "@/features/auth/store/authStore";

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { refetchOnWindowFocus: false, staleTime: 30_000 },
  },
});
// Account changes must discard cached private data, including conversations and learning history.
useAuthStore.subscribe((current, previous) => {
  if (current.user?.id !== previous.user?.id) queryClient.clear();
});

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <LanguageProvider>
        <BrowserRouter>
          <AuthBootstrap>
            <App />
          </AuthBootstrap>
        </BrowserRouter>
      </LanguageProvider>
    </QueryClientProvider>
  </StrictMode>,
);
