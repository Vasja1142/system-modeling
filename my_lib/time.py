import time
import functools

def func_time(func):
    @functools.wraps(func)
    def wrapper(*args, **kwargs):
        start = time.time()
        result = func(*args, **kwargs)
        end = time.time()
        print(f'{func.__name__} работала {(end - start)*1000} милисекунд')
        return result
    return wrapper