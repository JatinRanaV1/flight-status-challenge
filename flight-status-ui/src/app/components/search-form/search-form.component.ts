import { Component, EventEmitter, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { FlightStatusService } from '../../services/flight-status.service';
import { SearchState } from '../../models/flight-status.model';

@Component({
  selector: 'app-search-form',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './search-form.component.html',
  styleUrl: './search-form.component.css'
})
export class SearchFormComponent {
  @Output() stateChange = new EventEmitter<SearchState>();

  flightNumber = '';
  date         = '';
  loading      = false;

  constructor(private readonly flightStatusService: FlightStatusService) {}

  onSearch(): void {
    if (!this.flightNumber.trim() || !this.date) return;

    this.loading = true;
    this.stateChange.emit({ result: null, error: null, loading: true });

    this.flightStatusService.getStatus(this.flightNumber.trim().toUpperCase(), this.date)
      .subscribe({
        next: result => {
          this.loading = false;
          this.stateChange.emit({ result, error: null, loading: false });
        },
        error: (err: HttpErrorResponse) => {
          this.loading = false;
          const message = err.error?.error ?? `Error ${err.status}: ${err.statusText}`;
          this.stateChange.emit({ result: null, error: message, loading: false });
        }
      });
  }
}
