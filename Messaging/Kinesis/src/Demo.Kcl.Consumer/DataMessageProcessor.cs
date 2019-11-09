﻿using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using Amazon.Kinesis.ClientLibrary;
using Demo.Domain;

namespace Demo.Kcl.Consumer
{
    public class DataMessageProcessor : IShardRecordProcessor
    {
        /// <value>The time to wait before this record processor
        /// reattempts either a checkpoint, or the processing of a record.</value>
        private static readonly TimeSpan Backoff = TimeSpan.FromSeconds(3);

        /// <value>The interval this record processor waits between
        /// doing two successive checkpoints.</value>
        private static readonly TimeSpan CheckpointInterval = TimeSpan.FromMinutes(1);

        /// <value>The maximum number of times this record processor retries either
        /// a failed checkpoint, or the processing of a record that previously failed.</value>
        private static readonly int NumRetries = 10;

        /// <value>The shard ID on which this record processor is working.</value>
        private string _kinesisShardId;

        /// <value>The next checkpoint time expressed in milliseconds.</value>
        private DateTime _nextCheckpointTime = DateTime.UtcNow;

        /// <summary>
        /// This method is invoked by the Amazon Kinesis Client Library before records from the specified shard
        /// are delivered to this SampleRecordProcessor.
        /// </summary>
        /// <param name="input">
        /// InitializationInput containing information such as the name of the shard whose records this
        /// SampleRecordProcessor will process.
        /// </param>
        public void Initialize(InitializationInput input)
        {
            Console.Error.WriteLine("Initializing record processor for shard: " + input.ShardId);
            this._kinesisShardId = input.ShardId;
        }

        /// <summary>
        /// This method processes the given records and checkpoints using the given checkpointer.
        /// </summary>
        /// <param name="input">
        /// ProcessRecordsInput that contains records, a Checkpointer and contextual information.
        /// </param>
        public void ProcessRecords(ProcessRecordsInput input)
        {
            // Process records and perform all exception handling.
            ProcessRecordsWithRetries(input.Records);

            // Checkpoint once every checkpoint interval.
            if (DateTime.UtcNow >= _nextCheckpointTime)
            {
                Checkpoint(input.Checkpointer);
                _nextCheckpointTime = DateTime.UtcNow + CheckpointInterval;
            }
        }

        public void LeaseLost(LeaseLossInput leaseLossInput)
        {
            //
            // Perform any necessary cleanup after losing your lease.  Checkpointing is not possible at this point.
            //
            Console.Error.WriteLine($"Lost lease on {_kinesisShardId}");
        }

        public void ShardEnded(ShardEndedInput shardEndedInput)
        {
            //
            // Once the shard has ended it means you have processed all records on the shard. To confirm completion the
            // KCL requires that you checkpoint one final time using the default checkpoint value.
            //
            Console.Error.WriteLine(
                $"All records for {_kinesisShardId} have been processed, starting final checkpoint");
            shardEndedInput.Checkpointer.Checkpoint();
        }

        public void ShutdownRequested(ShutdownRequestedInput shutdownRequestedInput)
        {
            Console.Error.WriteLine($"Shutdown has been requested for {_kinesisShardId}. Checkpointing");
            shutdownRequestedInput.Checkpointer.Checkpoint();
        }

        /// <summary>
        /// This method processes records, performing retries as needed.
        /// </summary>
        /// <param name="records">The records to be processed.</param>
        private void ProcessRecordsWithRetries(List<Record> records)
        {
            foreach (Record record in records)
            {
                bool processedSuccessfully = false;
                DataMessage dataMessage = null;
                for (int i = 0; i < NumRetries; ++i)
                {
                    try
                    {
                        dataMessage = JsonSerializer.Deserialize<DataMessage>(record.Data);
                        Console.WriteLine($"Retrieved record:{Environment.NewLine}PartitionKey={record.PartitionKey}, SequenceNumber={record.SequenceNumber}{Environment.NewLine}DataMessage Id={dataMessage.Id}, CreatedOn={dataMessage.CreatedOn.ToString("yyyy-MM-dd HH:mm")}");

                        // Your own logic to process a record goes here.

                        processedSuccessfully = true;
                        break;
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine("Exception processing record data: " + dataMessage, e);
                    }

                    //Back off before retrying upon an exception.
                    Thread.Sleep(Backoff);
                }

                if (!processedSuccessfully)
                {
                    Console.Error.WriteLine("Couldn't process record " + record + ". Skipping the record.");
                }
            }
        }

        /// <summary>
        /// This checkpoints the specified checkpointer with retries.
        /// </summary>
        /// <param name="checkpointer">The checkpointer used to do checkpoints.</param>
        private void Checkpoint(Checkpointer checkpointer)
        {
            Console.Error.WriteLine("Checkpointing shard " + _kinesisShardId);

            // You can optionally provide an error handling delegate to be invoked when checkpointing fails.
            // The library comes with a default implementation that retries for a number of times with a fixed
            // delay between each attempt. If you do not provide an error handler, the checkpointing operation
            // will not be retried, but processing will continue.
            checkpointer.Checkpoint(RetryingCheckpointErrorHandler.Create(NumRetries, Backoff));
        }
    }
}