import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Format } from '../models/format.model';

@Injectable({ providedIn: 'root' })
export class FormatService {
  private readonly url = '/api/format';

  constructor(private http: HttpClient) {}

  getAll(): Observable<Format[]> {
    return this.http.get<Format[]>(`${this.url}`);
  }

  getById(id: number): Observable<Format> {
    return this.http.get<Format>(`${this.url}/${id}`);
  }

  create(format: Format): Observable<Format> {
    return this.http.post<Format>(this.url, format);
  }

  update(id: number, format: Format): Observable<void> {
    return this.http.put<void>(`${this.url}/${id}`, format);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }
}
