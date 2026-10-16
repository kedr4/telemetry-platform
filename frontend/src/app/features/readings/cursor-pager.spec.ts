import { CursorPager } from './cursor-pager';
import { localInputToIso } from './readings';

describe('CursorPager', () => {
  it('starts on the first page without a cursor', () => {
    const pager = CursorPager.first();

    expect(pager.pageIndex).toBe(0);
    expect(pager.after).toBeNull();
    expect(pager.hasPrevious).toBe(false);
  });

  it('remembers visited cursors to go back', () => {
    const third = CursorPager.first().next('c1').next('c2');

    expect(third.pageIndex).toBe(2);
    expect(third.after).toBe('c2');

    const second = third.previous();
    expect(second.after).toBe('c1');
    expect(second.previous().after).toBeNull();
  });

  it('stays on the first page when going back from it', () => {
    const first = CursorPager.first();

    expect(first.previous()).toBe(first);
  });

  it('is immutable', () => {
    const first = CursorPager.first();
    first.next('c1');

    expect(first.after).toBeNull();
  });
});

describe('localInputToIso', () => {
  it('converts a local date-time input to an ISO timestamp', () => {
    expect(localInputToIso('2026-10-01T12:30')).toBe(new Date('2026-10-01T12:30').toISOString());
  });

  it('treats empty or invalid input as no bound', () => {
    expect(localInputToIso('')).toBeNull();
    expect(localInputToIso('not a date')).toBeNull();
  });
});
