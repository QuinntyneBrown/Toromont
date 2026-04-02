export interface ApiResponse<T> {
  data: T;
  success: boolean;
  message?: string;
  errors?: string[];
  totalCount?: number;
  page?: number;
  pageSize?: number;
}
