import { Injectable, NgZone } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private baseUrl = '/api/v1';

  constructor(private http: HttpClient, private ngZone: NgZone) {}

  get<T>(path: string, params?: Record<string, string | number | boolean>): Observable<T> {
    let httpParams = new HttpParams();
    if (params) {
      Object.keys(params).forEach(key => {
        httpParams = httpParams.set(key, String(params[key]));
      });
    }
    return new Observable<T>(subscriber => {
      this.http.get<T>(`${this.baseUrl}${path}`, { params: httpParams }).pipe(
        catchError(this.handleError)
      ).subscribe({
        next: v => this.ngZone.run(() => subscriber.next(v)),
        error: e => this.ngZone.run(() => subscriber.error(e)),
        complete: () => this.ngZone.run(() => subscriber.complete())
      });
    });
  }

  post<T>(path: string, body: unknown): Observable<T> {
    return new Observable<T>(subscriber => {
      this.http.post<T>(`${this.baseUrl}${path}`, body).pipe(
        catchError(this.handleError)
      ).subscribe({
        next: v => this.ngZone.run(() => subscriber.next(v)),
        error: e => this.ngZone.run(() => subscriber.error(e)),
        complete: () => this.ngZone.run(() => subscriber.complete())
      });
    });
  }

  put<T>(path: string, body: unknown): Observable<T> {
    return new Observable<T>(subscriber => {
      this.http.put<T>(`${this.baseUrl}${path}`, body).pipe(
        catchError(this.handleError)
      ).subscribe({
        next: v => this.ngZone.run(() => subscriber.next(v)),
        error: e => this.ngZone.run(() => subscriber.error(e)),
        complete: () => this.ngZone.run(() => subscriber.complete())
      });
    });
  }

  delete<T>(path: string): Observable<T> {
    return new Observable<T>(subscriber => {
      this.http.delete<T>(`${this.baseUrl}${path}`).pipe(
        catchError(this.handleError)
      ).subscribe({
        next: v => this.ngZone.run(() => subscriber.next(v)),
        error: e => this.ngZone.run(() => subscriber.error(e)),
        complete: () => this.ngZone.run(() => subscriber.complete())
      });
    });
  }

  private handleError(error: unknown) {
    console.error('API Error:', error);
    return throwError(() => error);
  }
}
