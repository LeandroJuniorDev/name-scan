window.nameScanStream = {
  start: (nickname, dotNetRef) => {
    const source = new EventSource(`/api/check/stream?nickname=${encodeURIComponent(nickname)}`);
    const fallbackError = JSON.stringify({
      kind: 2,
      message: "Não foi possível concluir a verificação."
    });

    const closeSource = () => {
      source.close();
    };

    source.addEventListener("result", async (event) => {
      await dotNetRef.invokeMethodAsync("OnStreamResult", event.data);
    });

    source.addEventListener("done", async (event) => {
      await dotNetRef.invokeMethodAsync("OnStreamDone", event.data);
      closeSource();
    });

    source.addEventListener("error", async (event) => {
      const payload = typeof event.data === "string" && event.data.trim().length > 0
        ? event.data
        : fallbackError;
      await dotNetRef.invokeMethodAsync("OnStreamError", payload);
      closeSource();
    });

    return {
      stop: () => closeSource()
    };
  }
};
