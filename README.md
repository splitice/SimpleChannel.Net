# SimpleChannel.Net

A simple channel based communication framework

[![Build Status](https://travis-ci.org/splitice/SimpleChannel.Net.svg)](https://travis-ci.org/splitice/SimpleChannel.Net)

## Usage Example
```
void Example(IModel rabbitMqModel)
{
    IChannel<TObject> input = new Channel<TObject>();
    IChannel<TObject> output = new AmqpBasicQueueChannel<TObject>(model: rabbitMqModel, queue: "");

    while (true)
    {
        var workingObject = input.Take();
        var newObject = workingObject.DoProcessing();
        input.Ack();
        output.Put(newObject);
    }
}
```