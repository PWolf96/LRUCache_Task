# LRU Cache task

## Brief
The task asks to create an in-memory cache component that stores and retrieves arbitrary types of objects.

- The cache should implement a threshold.
- When the threshold is exceeded the add operation should succeed but another entry should be evicted based on LRU principle
- The component should be a Singleton
- Add logging to notify the user when an item is evicted


## Implementation

The implementation includes a mixture of a linked list and a dictionary. The dictionary holds the values and the linked list holds the order. Implementing a solution using a queue was also considered but decided against.

## Considerations

### Add a hysteresis to manage the threshold

A hysteresis or (an optimal range) is implemented to prevent frequent additions and evictions when the size of the cache fluctuates around the threshold. 

Two types were considered.

- Only an upper threshold - in this case the cache has a buffer which allows the size to be exceeded by a certain amount until it starts evicting back to the original threshold. This case would maintain the size of the cache near the maximum allowable threshold.
- Lower and upper threshold - Have a buffer on both sides of the "optimal" size of the cache. Once the upper threshold is exceeded the size is lowered to the lower threshold.

The second approach was implemented because it is assumed that the service is looking for stability and predictability of the size. By defining an "optimal" size and two thresholds around it the service is constantly trying to normalize the count around that optimal size which would make it more predictable.

### The evicting process

- A decision has been made to remove the items one by one, allowing some of the more frequently added items to still be accessed in case of an immediate query before they get removed. That could potentially improve the hit rate.
- A separate task is defined to evict tasks until the state of the hysteresis is satisfied. This will allow multiple threads to add to the cache without having to wait for the eviction process to complete. At the same time a boolean is added to make sure that only one eviction process is happening at the same time and prevent concurrency issues.
- A semaphore was considered for adding a more fine grained control over the number of threads managing gets/adds/evictions but it was assumed that the above approach will suffice.

### Managing concurrency
2 locks are introduced to manage concurrency. 
__cacheLock - this lock restricts the get and add methods to manipulate the cache at the same time. It ensures that at any given time an item is either queried, added or removed and keeps the state of the cache consistent
__evictionLock - this prevents from multiple eviction processes happening at the same time in case two threads enter EvictItem before the __isEvicting boolean is set

### Adding a hit rate metric

- A hit rate metric was added to allow for more observability around the efficacy of the cache.


## Further work

There are a few areas where the code would need to be extended to make it into a production setting:

- Test cases around multithreaded performance. More complicated test cases would have to be written to ensure that the concurrency is dealt with in an efficient and robust manner. Such tests include checking if any threads update the same value, checks around the get method and whether the items information corresponds to what should be expected at that point etc.
- More test cases around the performance of the caching solution. Different end to end tests can be implemented simulating 10,100,1000,10000.... items and recording the performance for each case. That information will help with the understanding of the limits of the solution.
- More tests around the hit rate would be helpful to confirm the accuracy of the cache data
- The current solution implements a simple console log in the event of an eviction. A production solution would tie that logic to an observability solution such as Grafana and alerts can be built around that
- As a result of the above two, further improvements can be made with regards to the amount of threads that are allowed at the same time. Various configurations using Semaphore can be explored to find the most efficient setup.
- The configuration can be shifted into a cloud provider or any other 3rd party to keep a single source of truth in the case of many images.


## Notes

- I spent approximately 3.5 hours on the task. When I read the task I knew I had 2 valid solutions - one with a linked list and one with a queue. I did a little bit of research to confirm which one was the more efficient and proceeded with that one.
- I knew I needed a hysteresis but was not sure which implementation to go with. I was debating on the trade-offs between the two approaches I described above because one could potentially result in items being removed when they shouldn't have (low,high threshold) and the other one could result in consistently higher utilization than needed. I did not consider the option where the actual threshold is the hard stop because I wanted to allow for a higher traffic scenario.
In the end I went with what I thought will produce the most predictable outcome for the system which will make it easier to provision resources.
- I spent some time on making the proper locks. In the end I decided to go with a more standard approach where only 1 action is allowed at the same time(get, add, evict).
I considered other approaches such as ConcurrentDictionary and ConcurrentQueues but decided to go with the more standard approach and not get into unnecessary complications and test cases for this task.
- I briefly considered using Semaphore to show a more fine-grained control over the process of get/add/evict but decided it is unnecessary for the task at hand.



