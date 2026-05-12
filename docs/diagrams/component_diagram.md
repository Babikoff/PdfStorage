# Диаграмма компонентов

```mermaid
graph TD
    A[Браузер] --> B[DocumentStorageWebApi]
    B --> C[Очередь]
    B --> D[База данных]
    C --> E[BackgroundFileProcessor]
    E --> D
```