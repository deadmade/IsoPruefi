#pragma once

struct MockDateTime {
  int _year, _month, _day, _hour, _minute, _second;

  MockDateTime(int year, int month, int day, int hour, int minute, int second = 0)
    : _year(year), _month(month), _day(day), _hour(hour), _minute(minute), _second(second) {}

  int year()   const { return _year; }
  int month()  const { return _month; }
  int day()    const { return _day; }
  int hour()   const { return _hour; }
  int minute() const { return _minute; }
  int second() const { return _second; }
};

#ifdef UNIT_TEST
  using DateTime = MockDateTime;
#endif
