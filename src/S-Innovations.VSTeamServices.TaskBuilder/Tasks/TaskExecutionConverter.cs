



namespace SInnovations.VSTeamServices.TaskBuilder.Tasks
{
    using System;
    using Models;
    using Newtonsoft.Json;

    public class TaskExecutionConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(TaskExecution) == objectType;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var task = value as TaskExecution;
            writer.WriteStartObject();
            writer.WritePropertyName(task.ExecutionType);
            writer.WriteStartObject();
            writer.WritePropertyName("target"); writer.WriteValue(task.Target);
            writer.WritePropertyName("workingDirectory"); writer.WriteValue(task.WorkingDirectory);
            writer.WritePropertyName("argumentFormat"); writer.WriteValue(task.ArgumentFormat ?? "");
            writer.WriteEndObject();
            writer.WriteEndObject();
        }
    }
}
