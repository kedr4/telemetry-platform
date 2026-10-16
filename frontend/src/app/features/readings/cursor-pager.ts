/**
 * Previous/next navigation over cursor-paginated results. The API only pages forward, so the cursors of
 * visited pages are remembered to go back. Immutable: every move returns a new pager.
 */
export class CursorPager {
  private constructor(private readonly cursors: readonly (string | null)[]) {}

  static first(): CursorPager {
    return new CursorPager([null]);
  }

  /** Zero-based index of the current page. */
  get pageIndex(): number {
    return this.cursors.length - 1;
  }

  /** Cursor to request the current page with (`null` for the first page). */
  get after(): string | null {
    return this.cursors[this.cursors.length - 1];
  }

  get hasPrevious(): boolean {
    return this.cursors.length > 1;
  }

  next(endCursor: string): CursorPager {
    return new CursorPager([...this.cursors, endCursor]);
  }

  previous(): CursorPager {
    return this.hasPrevious ? new CursorPager(this.cursors.slice(0, -1)) : this;
  }
}
