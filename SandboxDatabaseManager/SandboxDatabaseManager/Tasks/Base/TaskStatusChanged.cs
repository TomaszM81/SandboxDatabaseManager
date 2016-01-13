namespace SandboxDatabaseManager.Tasks
{
    public delegate void TaskStatusChanged(string Owner, string taskId, TaskStatus status);
}