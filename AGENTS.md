# Guidance for CPP repository

* The domain entity `Task` lives in `Models.Models.Task`. When working in this repository, prefer explicit namespaces or aliases (e.g., `using TaskEntity = Models.Models.Task;`) to avoid confusion with `System.Threading.Tasks.Task`, and clarify which one you mean in discussions.
