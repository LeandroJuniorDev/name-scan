window.nameScanStream = {
  start: (nickname, dotNetRef) => {
    const source = new EventSource(`/api/check/stream?nickname=${encodeURIComponent(nickname)}`);

    source.addEventListener("result", async (event) => {
      await dotNetRef.invokeMethodAsync("OnStreamResult", event.data);
    });

    source.addEventListener("done", async (event) => {
      await dotNetRef.invokeMethodAsync("OnStreamDone", event.data);
      source.close();
    });

    source.addEventListener("error", async (event) => {
      if (event.data) {
        await dotNetRef.invokeMethodAsync("OnStreamError", event.data);
      }

      source.close();
    });

    return {
      stop: () => source.close()
    };
  }
};
