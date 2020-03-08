#!/bin/sh

set -e

until aws --region eu-west-1 --endpoint-url=http://localstack:4576 sqs list-queues; do
  >&2 echo "Localstack SQS is unavailable - sleeping"
  sleep 1
done

>&2 echo "Localstack SQS is up - executing command"
aws --region eu-west-1 --endpoint-url=http://localstack:4576 sqs create-queue --queue-name producer-host
aws --region eu-west-1 --endpoint-url=http://localstack:4576 sqs create-queue --queue-name producer-consumer
aws --region eu-west-1 --endpoint-url=http://localstack:4576 sqs create-queue --queue-name consumer-host
aws --region eu-west-1 --endpoint-url=http://localstack:4576 sqs create-queue --queue-name error
aws --region eu-west-1 --endpoint-url=http://localstack:4572 s3api create-bucket --bucket bucketname
aws --endpoint-url=http://localstack:4572 s3api put-bucket-acl --bucket bucketname --acl public-read