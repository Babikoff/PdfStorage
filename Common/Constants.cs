namespace Common
{
    public static class Constants
    {
        /// <summary>
        /// Максимально допустимый размер принимаемого файла в байтах.
        /// </summary>
        public const int MaxDocumentSize = 10 * 1024 * 1024;

        /// <summary>
        /// Задержка обработки сообщения при тестировании
        /// </summary>
#if DEBUG        
        public const int TestQueueMessageConsumingDelay = 20000;
#else
        public const int TestQueueMessageConsumingDelay = 0;
#endif
    }
}
